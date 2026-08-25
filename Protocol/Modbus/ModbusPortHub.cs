using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using NModbus.Data;
using NModbus.Device;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>
    /// Modbus TCP 共享传输层：每个端口只建立一个 TcpListener 与一个 NModbus SlaveNetwork，
    /// 同端口不同从站号注册为独立从站；同端口同从站号的多个设备共享同一个从站寄存器镜像
    /// （挂载前经 <see cref="AddressOverlapValidator"/> 校验地址不重叠）。
    /// </summary>
    public sealed class ModbusPortHub
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ModbusPortHub));
        private static readonly Lazy<ModbusPortHub> _instance = new(() => new ModbusPortHub());

        /// <summary>全局共享实例（托管服务、从站与 Web 端点共用）。</summary>
        public static ModbusPortHub Instance => _instance.Value;

        private readonly object _gate = new();
        private readonly Dictionary<int, PortListenerHost> _hosts = new();

        /// <summary>设备挂载结果：失败时 Errors 给出具体冲突/绑定原因。</summary>
        public sealed class AttachResult
        {
            public bool Ok { get; init; }
            public List<string> Errors { get; init; } = new();
            /// <summary>挂载成功后的共享寄存器镜像，供从站挂载控制写钩子。</summary>
            public SlaveDataStore? DataStore { get; init; }

            public static AttachResult Success(SlaveDataStore store) =>
                new() { Ok = true, DataStore = store };

            public static AttachResult Fail(params string[] errors) =>
                new() { Ok = false, Errors = errors.ToList() };
        }

        /// <summary>
        /// 将设备点表挂载到 (端口, 从站号) 的共享从站上。
        /// 先与同从站号下已挂载设备做地址查重，再确保监听与从站存在。
        /// </summary>
        public AttachResult AttachDevice(int port, byte slaveId, string deviceName, MapEntry[] entries)
        {
            lock (_gate)
            {
                var host = EnsureHost(port, out string? bindError);
                if (host == null)
                    return AttachResult.Fail(bindError ?? $"端口 {port} 绑定失败");

                var slot = host.GetOrCreateSlot(slaveId);

                var probe = new List<AddressOverlapValidator.DevicePointMap>();
                foreach (var attached in slot.Devices)
                    probe.Add(new AddressOverlapValidator.DevicePointMap(attached.Key, slaveId, attached.Value));
                probe.Add(new AddressOverlapValidator.DevicePointMap(deviceName, slaveId, entries));

                var conflicts = AddressOverlapValidator.Validate(probe);
                if (conflicts.Count > 0)
                {
                    var prefix = $"端口 {port} ";
                    return AttachResult.Fail(conflicts.Select(c => prefix + c).ToArray());
                }

                slot.Devices[deviceName] = entries;
                return AttachResult.Success(slot.DataStore);
            }
        }

        /// <summary>
        /// 卸载设备：设备是所在从站的最后一个成员时移除从站；从站清空且端口无其它从站时释放监听。
        /// </summary>
        public void DetachDevice(int port, byte slaveId, string deviceName)
        {
            lock (_gate)
            {
                if (!_hosts.TryGetValue(port, out var host))
                    return;

                if (host.TryGetSlot(slaveId, out var slot))
                {
                    slot!.Devices.Remove(deviceName);
                    if (slot.Devices.Count == 0)
                        host.RemoveSlot(slaveId);
                }

                if (host.SlotCount == 0)
                {
                    host.Dispose();
                    _hosts.Remove(port);
                    Log.Info($"端口 {port} 已无挂载设备，释放 Modbus 监听");
                }
            }
        }

        public SlaveDataStore? GetDataStore(int port, byte slaveId)
        {
            lock (_gate)
            {
                return _hosts.TryGetValue(port, out var host) && host.TryGetSlot(slaveId, out var slot)
                    ? slot!.DataStore
                    : null;
            }
        }

        public NModbus.IModbusSlaveNetwork? GetNetwork(int port)
        {
            lock (_gate)
            {
                return _hosts.TryGetValue(port, out var host) ? host.Network : null;
            }
        }

        public bool IsPortListening(int port)
        {
            lock (_gate)
            {
                return _hosts.TryGetValue(port, out var host) && host.IsListening;
            }
        }

        /// <summary>挂载到某端口的设备清单（快照），供界面展示共享组。</summary>
        public Dictionary<byte, List<string>> GetAttachedDevices(int port)
        {
            lock (_gate)
            {
                var result = new Dictionary<byte, List<string>>();
                if (!_hosts.TryGetValue(port, out var host))
                    return result;
                foreach (var (slaveId, slot) in host.Slots)
                    result[slaveId] = slot.Devices.Keys.ToList();
                return result;
            }
        }

        /// <summary>释放全部监听与从站网络（进程退出 / 协议层重建前调用）。</summary>
        public void ShutdownAll()
        {
            lock (_gate)
            {
                foreach (var host in _hosts.Values)
                {
                    try { host.Dispose(); }
                    catch (Exception ex) { Log.Warn($"释放端口 {host.Port} 监听时异常", ex); }
                }
                _hosts.Clear();
            }
        }

        private PortListenerHost? EnsureHost(int port, out string? error)
        {
            if (_hosts.TryGetValue(port, out var existing))
            {
                error = null;
                return existing;
            }

            var host = new PortListenerHost(port);
            try
            {
                host.Start();
            }
            catch (Exception ex)
            {
                Log.Error($"端口 {port} Modbus 监听启动失败：{ex.Message}");
                error = $"端口 {port} 监听启动失败：{ex.Message}";
                return null;
            }

            _hosts[port] = host;
            Log.Info($"端口 {port} Modbus 共享监听已启动");
            error = null;
            return host;
        }

        /// <summary>单个端口的监听宿主：一个 TcpListener + 一个 NModbus 从站网络。</summary>
        private sealed class PortListenerHost : IDisposable
        {
            public int Port { get; }
            public bool IsListening { get; private set; }
            public NModbus.IModbusSlaveNetwork? Network { get; private set; }

            private readonly TcpListener _listener;
            private readonly NModbus.ModbusFactory _factory = new();
            private readonly Dictionary<byte, SharedSlaveSlot> _slots = new();

            public PortListenerHost(int port)
            {
                Port = port;
                _listener = new TcpListener(IPAddress.Any, port);
            }

            public int SlotCount => _slots.Count;

            public IEnumerable<KeyValuePair<byte, SharedSlaveSlot>> Slots => _slots;

            public void Start()
            {
                _listener.Start();
                IsListening = true;
                Network = _factory.CreateSlaveNetwork(_listener);
                Network.ListenAsync();
            }

            public SharedSlaveSlot GetOrCreateSlot(byte slaveId)
            {
                if (_slots.TryGetValue(slaveId, out var slot))
                    return slot;

                slot = new SharedSlaveSlot(slaveId);
                var nmodbusSlave = _factory.CreateSlave(slaveId, slot.DataStore);
                Network!.AddSlave(nmodbusSlave);
                _slots[slaveId] = slot;
                return slot;
            }

            public bool TryGetSlot(byte slaveId, out SharedSlaveSlot? slot)
            {
                if (_slots.TryGetValue(slaveId, out var found))
                {
                    slot = found;
                    return true;
                }
                slot = null;
                return false;
            }

            public void RemoveSlot(byte slaveId)
            {
                if (!_slots.Remove(slaveId))
                    return;
                try { Network?.RemoveSlave(slaveId); }
                catch { /* 从站网络可能已释放 */ }
            }

            public void Dispose()
            {
                IsListening = false;
                _slots.Clear();
                try { Network?.Dispose(); }
                catch { /* 忽略释放异常 */ }
                Network = null;
                try { _listener.Stop(); }
                catch { /* 忽略释放异常 */ }
            }
        }

        /// <summary>同 (端口, 从站号) 的共享从站槽位：一个寄存器镜像 + 多个挂载设备。</summary>
        public sealed class SharedSlaveSlot
        {
            public SharedSlaveSlot(byte slaveId)
            {
                SlaveId = slaveId;
                DataStore = new SlaveDataStore();
            }

            public byte SlaveId { get; }
            public SlaveDataStore DataStore { get; }
            /// <summary>设备名 -> 该设备挂载的点表条目（用于后续挂载时的地址查重）。</summary>
            public Dictionary<string, MapEntry[]> Devices { get; } = new(StringComparer.OrdinalIgnoreCase);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using log4net;
using static EssSimulator.EssDeviceSimModel.EnergyStorageSystem;

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>
    /// 负责 Modbus 寄存器的数据同步调度：
    ///   - 按 modelType 分组，每组一个 Worker 周期写入数据寄存器
    ///   - 独立控制线程（syncThread）轮询控制寄存器，有变化时回写模型
    /// </summary>
    public class ModbusDataSync
    {
        private readonly IModbusSlave   _slave;
        private readonly ModbusParser   _parser;
        private readonly ModbusPointMap _map;
        private readonly DeviceInfoDto  _deviceInfo;
        private readonly int            _clusterCount;
        private readonly ILog           _log = LogManager.GetLogger(typeof(ModbusDataSync));

        // 使用 ConcurrentDictionary 避免多 Worker 线程并发读写时的竞态
        private readonly ConcurrentDictionary<string, object?> _shadowData    = new();
        private readonly ConcurrentDictionary<string, object?> _shadowControl = new();
        private readonly ConcurrentDictionary<string, object?>[]? _rackShadowData;
        private readonly ConcurrentDictionary<string, object?>[]? _rackShadowControl;

        private volatile bool _running;
        private Thread?       _controlThread;
        private readonly Dictionary<string, Thread> _workerThreads = new();

        public ModbusDataSync(IModbusSlave slave, ModbusParser parser, ModbusPointMap map,
                              DeviceInfoDto deviceInfo, int clusterCount)
        {
            _slave        = slave;
            _parser       = parser;
            _map          = map;
            _deviceInfo   = deviceInfo;
            _clusterCount = clusterCount;

            // 初始化主设备 shadow 缓存
            foreach (var e in map.DataMaps)
                _shadowData.TryAdd(e.ParamName!, 0);
            foreach (var e in map.ControlMaps)
                _shadowControl.TryAdd(e.ParamName!, 0);

            // 初始化 rack shadow 缓存
            if (clusterCount > 0)
            {
                _rackShadowData    = new ConcurrentDictionary<string, object?>[clusterCount];
                _rackShadowControl = new ConcurrentDictionary<string, object?>[clusterCount];
                for (int i = 0; i < clusterCount; i++)
                {
                    _rackShadowData[i]    = new();
                    _rackShadowControl[i] = new();
                    foreach (var e in map.RackDataMaps)
                        _rackShadowData[i].TryAdd(e.ParamName!, 0);
                    foreach (var e in map.RackControlMaps)
                        _rackShadowControl[i].TryAdd(e.ParamName!, 0);
                }
            }
        }

        // ── 生命周期 ───────────────────────────────────────────────

        /// <summary>写入默认值后启动所有 Worker 和控制线程。</summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            // 1) 写入 CSV 常量默认值
            _slave.Write(_map.DefaultBuffer);

            // 2) 控制点按对象当前值回填寄存器，确保“对象默认值”与“点位初值”一致
            try
            {
                var initCtl = new Dictionary<string, object>();
                foreach (var e in _map.ControlMaps)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.ParamName)) continue;
                    if (!_map.ParamModelLookup.TryGetValue(e.ParamName, out var model) || model == null) continue;
                    if (string.IsNullOrWhiteSpace(model.Arg1)) continue;
                    var cur = SimServer.GetExtIfVariableVal(model.Arg1);
                    if (cur != null) initCtl[e.ParamName] = cur;
                }
                if (initCtl.Count > 0) _slave.Write(initCtl);
            }
            catch (Exception ex)
            {
                _log.Warn("Init control register defaults failed.", ex);
            }
            StartModelWorkers();
            StartControlThread();
        }

        public void Stop()
        {
            _running = false;
            foreach (var kv in _workerThreads)
                try { if (kv.Value.IsAlive) kv.Value.Join(2000); } catch { }
            _workerThreads.Clear();
            if (_controlThread != null && _controlThread.IsAlive)
                _controlThread.Join(2000);
        }

        // ── Worker 线程（数据寄存器写入）────────────────────────────

        private void StartModelWorkers()
        {
            foreach (var kv in _map.ModelParamLookup)
            {
                var modelType = kv.Key;
                var entries   = kv.Value;
                var t = new Thread(() => DataWorkerLoop(modelType, entries))
                    { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                _workerThreads[modelType] = t;
                t.Start();
            }

            foreach (var kv in _map.RackModelParamLookup)
            {
                var modelType = kv.Key;
                var entries   = kv.Value;
                var t = new Thread(() => RackWorkerLoop(modelType, entries))
                    { IsBackground = true, Priority = ThreadPriority.BelowNormal };
                _workerThreads[modelType + "_rack"] = t;
                t.Start();
            }
        }

        private void DataWorkerLoop(string modelType, List<MapEntry> entries)
        {
            const int refreshMs = 500;
            if (entries == null || entries.Count == 0) return;

            while (_running)
            {
                var writeBuffer = new Dictionary<string, object>();
                try
                {
                    foreach (var entry in entries)
                    {
                        if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;
                        var val = GetModelValueStub(entry);
                        if (!_shadowData.TryGetValue(entry.ParamName, out var prev) || !object.Equals(prev, val))
                        {
                            _shadowData[entry.ParamName] = val;
                            writeBuffer[entry.ParamName] = val;
                        }
                    }
                    if (writeBuffer.Count > 0) _slave.Write(writeBuffer);
                }
                catch (Exception ex) { _log.Error($"Worker [{modelType}] error", ex); }
                Thread.Sleep(refreshMs);
            }
        }

        private void RackWorkerLoop(string modelType, List<MapEntry> entries)
        {
            const int refreshMs = 500;
            if (entries == null || entries.Count == 0) return;

            while (_running)
            {
                try
                {
                    for (int rackId = 0; rackId < _clusterCount; rackId++)
                    {
                        var writeBuffer = new Dictionary<string, object>();
                        byte sid = (byte)(_deviceInfo.slaveId + rackId + 1);
                        foreach (var entry in entries)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;
                            var val = GetModelValueStubForRack(entry, rackId);
                            if (!_rackShadowData![rackId].TryGetValue(entry.ParamName, out var prev) || !object.Equals(prev, val))
                            {
                                _rackShadowData[rackId][entry.ParamName] = val;
                                writeBuffer[entry.ParamName] = val;
                            }
                        }
                        if (writeBuffer.Count > 0) _slave.Write(writeBuffer, sid);
                    }
                }
                catch (Exception ex) { _log.Error($"RackWorker [{modelType}] error", ex); }
                Thread.Sleep(refreshMs);
            }
        }

        // ── 控制线程（控制寄存器轮询回写）──────────────────────────

        private void StartControlThread()
        {
            _controlThread = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        if (_map.ControlMaps == null || _map.ControlMaps.Count == 0)
                        {
                            Thread.Sleep(1000);
                            continue;
                        }

                        var allRaw = _slave.Read();
                        if (allRaw == null || allRaw.Count == 0) { Thread.Sleep(1000); continue; }

                        var selectedRaw = new Dictionary<string, object>();
                        foreach (var entry in _map.ControlMaps)
                        {
                            if (entry == null || string.IsNullOrWhiteSpace(entry.ParamName)) continue;
                            if (allRaw.TryGetValue(entry.ParamName, out var raw))
                                selectedRaw[entry.ParamName] = raw;
                        }
                        if (selectedRaw.Count == 0) { Thread.Sleep(5000); continue; }

                        var parsed = _parser.DataParse(selectedRaw);
                        foreach (var kv in parsed)
                        {
                            var paramName = kv.Key;
                            var newValue  = kv.Value;
                            if (!_shadowControl.TryGetValue(paramName, out var prev) || !object.Equals(prev, newValue))
                            {
                                SetDataObjectByMesurePointName(paramName, newValue);
                                _shadowControl[paramName] = newValue;
                            }
                        }
                    }
                    catch (Exception ex) { _log.Error("Control thread error", ex); }

                    // 每轮轮询后休眠，避免控制线程在“无变化”时忙等占满 CPU，影响其它 Worker/GUI。
                    Thread.Sleep(100);
                }
            }) { IsBackground = true, Priority = ThreadPriority.BelowNormal };
            _controlThread.Start();
        }

        // ── 值解析 stub ─────────────────────────────────────────────

        private object GetModelValueStub(MapEntry entry)
        {
            if (!_map.ParamModelLookup.TryGetValue(entry.ParamName!, out var model)) return 0;

            if (!int.TryParse(model.ModelType, out int modelType))
            {
                var tmp = SimServer.GetExtIfVariableVal(model.Arg1!);
                return tmp ?? 0.0f;
            }

            return modelType switch
            {
                4 => SimServer.GetExtIfVariableVal(model.Arg1!) ?? 0,
                _ => 0
            };
        }

        private object GetModelValueStubForRack(MapEntry entry, int rackId)
        {
            if (!_map.RackParamModelLookup.TryGetValue(entry.ParamName!, out var model)) return 0;

            string? arg1 = model.Arg1?.Replace("rackId", rackId.ToString());

            if (!int.TryParse(model.ModelType, out int modelType))
            {
                var tmp = SimServer.GetExtIfVariableVal(arg1!);
                return tmp ?? 0;
            }

            return modelType switch
            {
                4 => SimServer.GetExtIfVariableVal(arg1!) ?? 0,
                _ => 0
            };
        }

        // ── 外部接口 ────────────────────────────────────────────────

        public void SetDataObjectByMesurePointName(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!_map.ParamModelLookup.TryGetValue(name, out var model)) return;

            object valToSet = value;
            if (value is string s)
            {
                if (double.TryParse(s, out var dv))      valToSet = dv;
                else if (bool.TryParse(s, out var bv))   valToSet = bv ? 1 : 0;
            }

            valToSet = CoerceControlValueForTarget(model.Arg1!, valToSet, name);

            _shadowControl[name] = valToSet;
            if (!SimServer.SetExtIfVariableVal(model.Arg1!, valToSet))
            {
                _log.Warn($"SetExtIfVariableVal failed: {model.Arg1} <= {valToSet}");
                return;
            }

            if (name is "pcs1_startstop" or "pcs2_startstop"
                or "pcs1_blackstart_enable" or "pcs2_blackstart_enable"
                or "param64" or "param65")
                TryApplyPcsCommandsImmediately();
        }

        /// <summary>线圈/寄存器写入目标属性时做类型对齐（避免 int 1 无法写入 bool 导致 Modbus 已为 1 但 emu 仍为 false）。</summary>
        private object CoerceControlValueForTarget(string argPath, object valToSet, string paramName)
        {
            var ctlEntry = _map.ControlMaps.Find(e => e != null && e.ParamName == paramName);
            bool isCoil = ctlEntry?.FunctionCode == 5;

            bool coilBool = valToSet switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var bv) => bv,
                _ => Convert.ToDouble(valToSet) != 0
            };

            if (!isCoil)
                return valToSet;

            try
            {
                var current = SimServer.GetExtIfVariableVal(argPath);
                if (current is bool)
                    return coilBool;
            }
            catch { /* ignore */ }

            return coilBool ? 1 : 0;
        }

        /// <summary>启停点位变化后立即驱动 PCS，不等待 PcsDataServer 下一周期。</summary>
        private void TryApplyPcsCommandsImmediately()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_deviceInfo.name) ||
                    !_deviceInfo.name.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!int.TryParse(_deviceInfo.name.AsSpan(6), out int unit1Based) || unit1Based < 1)
                    return;

                int unit0 = unit1Based - 1;
                var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
                var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{unit1Based}");
                if (ess == null || emu == null) return;

                PcsMapper.ApplyEmuCommands(emu, ess, unit0 * 2);
            }
            catch (Exception ex)
            {
                _log.Warn("TryApplyPcsCommandsImmediately failed.", ex);
            }
        }

        public object? GetDataObjectByMesurePointName(string name, IModbusSlave slave, ModbusParser parser)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var raw = slave.Read(name);
            if (raw == null) return null;
            var parsed = parser.DataParse(new Dictionary<string, object> { { name, raw } });
            return parsed.TryGetValue(name, out var val) ? val : null;
        }

        /// <summary>
        /// 标记数据点为“脏”，使下一轮数据 worker 强制回写实时值。
        /// 用于 dpc set 这类临时覆盖场景，避免因 shadow 去重而长期停留在手工值。
        /// </summary>
        public void InvalidateDataShadow(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _shadowData.TryRemove(name, out _);
        }

        /// <summary>
        /// 将控制量（线圈/寄存器）从模型回写到 Modbus 从站，并同步控制 shadow，便于外部工具读到与仿真一致的状态。
        /// </summary>
        public void PublishControlToSlave(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            SetDataObjectByMesurePointName(name, value);
            _shadowControl[name] = value;

            try
            {
                _slave.Write(new Dictionary<string, object> { { name, value } });
            }
            catch (Exception ex)
            {
                _log.Warn($"PublishControlToSlave failed for {name}: {ex.Message}");
            }
        }
    }
}

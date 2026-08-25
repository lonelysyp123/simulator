using System.Linq;
using EssSimulator.Configuration;
using EssSimulator.DataExchange;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;
using log4net;

namespace EssSimulator.LocalControl
{
    /// <summary>
    /// LocalControl 专用 Modbus TCP 从站：默认只维护 lc.csv 寄存器镜像（纯转发场景）；
    /// 点表含模型绑定时（如 trina 系统级点表）自动升级为 <see cref="DataExchangeSession"/> 驱动。
    /// 传输层由 <see cref="ModbusPortHub"/> 统一提供，可与其它设备共享端口/从站号。
    /// </summary>
    public sealed class LocalControlModbusServer : IModbusRegisterServer, IProtocolLayerServer
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LocalControlModbusServer));
        private readonly IModbusSlave _slave;
        private readonly ModbusParser _parser;
        private readonly DeviceInfoDto _deviceInfo;
        private readonly ModbusPointMap _pointMap;
        private readonly IModbusSyncBackend _backend;

        /// <summary>
        /// 构造 LC 从站。<paramref name="firstEmuId"/> 为聚合组首机组号（非空时把点表中的
        /// emuDeviceId 占位符替换为该机组根路径，LC 控制点作用于首机组 EMU 虚拟模型）。
        /// </summary>
        public LocalControlModbusServer(
            string mapFilePath,
            int modbusPort,
            string serverName,
            int? firstEmuId = null,
            IReadOnlyList<EssUnitConfig>? essUnits = null)
        {
            _pointMap = new ModbusPointMap(mapFilePath, serverName, clusterCount: 0, emuDeviceIdOverride: firstEmuId);
            _deviceInfo = new DeviceInfoDto
            {
                ip = "0.0.0.0",
                port = modbusPort,
                slaveId = 1,
                connectType = "ModbusTCP",
                collectionCycle = 1000,
                name = serverName
            };

            _slave = new ModbusTCPSlave(_deviceInfo, _pointMap.RawMaps, rackCount: 0);
            _parser = new ModbusParser(_pointMap.RawMaps);

            if (RequiresDataExchange(_pointMap))
            {
                UsesDataExchange = true;
                var catalog = PointCatalogLoader.FromPointMap(_pointMap, serverName, essUnits: essUnits);
                _backend = new DataExchangeSession(
                    _slave, _parser, catalog, _deviceInfo, new DataExchangeOptions(), clusterCount: 0);
                var emuHint = firstEmuId is int id ? $"（首机组 emu{id}）" : string.Empty;
                _log.Info($"{serverName} 点表含模型绑定，启用 DataExchange 管道{emuHint}");
            }
            else
            {
                _backend = new RegisterOnlyBackend(_slave, _parser, _pointMap);
            }
        }

        /// <summary>点表含模型绑定、由 DataExchange 管道驱动（桥接引擎应跳过此类设备）。</summary>
        public bool UsesDataExchange { get; }

        /// <summary>点表存在遥测模型绑定或控制模型绑定时需要 DataExchange 驱动。</summary>
        private static bool RequiresDataExchange(ModbusPointMap pointMap) =>
            pointMap.DataMaps.Any(m =>
            {
                var model = ModbusSimServer.GetModelParam(m.ModelSim);
                return model != null && !string.IsNullOrWhiteSpace(model.ModelType);
            })
            || pointMap.ControlMaps.Any(m =>
            {
                var model = ModbusSimServer.GetModelParam(m.ModelSim);
                return model != null && !string.IsNullOrWhiteSpace(model.Arg1);
            });

        public string ServerName => _deviceInfo.name ?? string.Empty;

        public bool IsOnline => _slave.GetCommunicatorState();

        public int Port => _deviceInfo.port;

        public byte SlaveId => _deviceInfo.slaveId;

        public int RackCount => 0;

        /// <summary>已加载的点表，供协议层地址查重使用。</summary>
        public ModbusPointMap PointMap => _pointMap;

        /// <summary>调整端口/从站号：要求先离线，由协议层管理器在重建时调用。</summary>
        public void Reconfigure(int port, byte slaveId)
        {
            if (IsOnline)
            {
                _log.Warn($"{ServerName} 在线时调整端口/从站号被忽略，请先停止服务");
                return;
            }
            _deviceInfo.port = port;
            _deviceInfo.slaveId = slaveId;
        }

        public IReadOnlyList<MapEntry> DataMaps => _pointMap.DataMaps;

        public IReadOnlyList<MapEntry> ControlMaps => _pointMap.ControlMaps;

        public IReadOnlyList<MapEntry> RackControlMaps => _pointMap.RackControlMaps;

        public void SetDataObjectByMesurePointName(string name, object value) =>
            _backend.SetDataObjectByMesurePointName(name, value);

        public bool TrySetRackControl(int rackIndex, string name, object value, out string message) =>
            _backend.TrySetRackControl(rackIndex, name, value, out message);

        public bool Start(int maxRetries = 30)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (attempt > 1)
                    Thread.Sleep(1000);

                _slave.DeviceConnect();
                if (_slave.GetCommunicatorState())
                {
                    _backend.Start();
                    return true;
                }

                _log.Warn($"{ServerName} 连接失败，第 {attempt}/{maxRetries} 次重试...");
            }

            _log.Error($"{ServerName} 经 {maxRetries} 次重试仍无法连接，放弃启动。");
            return false;
        }

        public void Stop()
        {
            try { _backend.Stop(); }
            catch (Exception ex) { _log.Error("Stop error", ex); }
            _slave.DeviceDisconnect();
        }

        public void PublishControlToSlave(string name, object value) =>
            _backend.PublishControlToSlave(name, value);

        public void SetDataStoreByMesurePointName(string name, object value)
        {
            var buf = new Dictionary<string, object> { { name, value } };
            try { _slave.Write(buf); }
            catch (Exception ex) { _log.Error("即时写入 Modbus 失败", ex); }
        }

        public object? GetDataObjectByMesurePointName(string name) =>
            _backend.GetDataObjectByMesurePointName(name, _slave, _parser);
    }
}

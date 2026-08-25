using EssSimulator.Protocol.Modbus;
using log4net;

namespace EssSimulator.LocalControl
{
    /// <summary>
    /// LocalControl 专用 Modbus TCP 从站：只维护 lc.csv 寄存器镜像，不参与仿真数据交换。
    /// 传输层由 <see cref="ModbusPortHub"/> 统一提供，可与其它设备共享端口/从站号。
    /// </summary>
    public sealed class LocalControlModbusServer : IModbusRegisterServer, IProtocolLayerServer
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LocalControlModbusServer));
        private readonly IModbusSlave _slave;
        private readonly ModbusParser _parser;
        private readonly DeviceInfoDto _deviceInfo;
        private readonly ModbusPointMap _pointMap;
        private readonly RegisterOnlyBackend _backend;

        public LocalControlModbusServer(string mapFilePath, int modbusPort, string serverName)
        {
            _pointMap = new ModbusPointMap(mapFilePath, serverName, clusterCount: 0);
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
            _backend = new RegisterOnlyBackend(_slave, _parser, _pointMap);
        }

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

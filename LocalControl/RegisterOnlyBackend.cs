using EssSimulator.Protocol.Modbus;

namespace EssSimulator.LocalControl
{
    /// <summary>
    /// 纯寄存器后端：仅写入 CSV 默认值，不绑定仿真模型、不启动轮询线程。
    /// </summary>
    internal sealed class RegisterOnlyBackend : IModbusSyncBackend
    {
        private readonly IModbusSlave _slave;
        private readonly ModbusParser _parser;
        private readonly ModbusPointMap _map;

        public RegisterOnlyBackend(IModbusSlave slave, ModbusParser parser, ModbusPointMap map)
        {
            _slave = slave;
            _parser = parser;
            _map = map;
        }

        public void Start() => _slave.Write(_map.DefaultBuffer);

        public void Stop() { }

        public void SetDataObjectByMesurePointName(string name, object value)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            _slave.Write(new Dictionary<string, object> { { name, value } });
        }

        public bool TrySetRackControl(int rackIndex, string name, object value, out string message)
        {
            message = "LocalControl 寄存器后端不支持簇级控制点";
            return false;
        }

        public void PublishControlToSlave(string name, object value) =>
            SetDataObjectByMesurePointName(name, value);

        public void InvalidateDataShadow(string name) { }

        public object? GetDataObjectByMesurePointName(string name, IModbusSlave slave, ModbusParser parser)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var raw = slave.Read(name);
            if (raw == null)
                return null;

            var parsed = parser.DataParse(new Dictionary<string, object> { { name, raw } });
            return parsed.TryGetValue(name, out var val) ? val : null;
        }
    }
}

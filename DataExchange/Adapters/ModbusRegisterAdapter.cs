using EssSimulator;

namespace EssSimulator.DataExchange.Adapters
{
    public sealed class ModbusRegisterAdapter : IModbusRegisterAdapter
    {
        private readonly IModbusSlave _slave;
        private readonly ModbusParser _parser;

        public ModbusRegisterAdapter(IModbusSlave slave, ModbusParser parser)
        {
            _slave = slave;
            _parser = parser;
        }

        public void WriteDefaults(IReadOnlyDictionary<string, object> defaults)
        {
            if (defaults.Count == 0)
                return;

            WriteWithSuppressedNotifications(new Dictionary<string, object>(defaults));
        }

        public void WritePoints(IReadOnlyDictionary<string, object> values, byte slaveId = 1)
        {
            if (values.Count == 0)
                return;

            WriteWithSuppressedNotifications(new Dictionary<string, object>(values), slaveId);
        }

        private void WriteWithSuppressedNotifications(Dictionary<string, object> values, byte slaveId = 1)
        {
            if (_slave is ModbusSlave modbusSlave)
            {
                using (modbusSlave.SuppressWriteNotifications())
                    _slave.Write(values, slaveId);
                return;
            }

            _slave.Write(values, slaveId);
        }

        public Dictionary<string, object> ReadAllControlRaw(IReadOnlyList<string> paramNames)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var allRaw = _slave.Read();
            if (allRaw == null || allRaw.Count == 0)
                return result;

            foreach (var name in paramNames)
            {
                if (allRaw.TryGetValue(name, out var raw))
                    result[name] = raw;
            }

            return result;
        }

        public object? ReadParsedPoint(string paramName, byte slaveId = 1)
        {
            var raw = _slave.Read(paramName);
            if (raw == null)
                return null;

            var parsed = _parser.DataParse(new Dictionary<string, object> { { paramName, raw } });
            return parsed.TryGetValue(paramName, out var val) ? val : null;
        }
    }
}

namespace EssSimulator.DataExchange.Adapters
{
    public interface IModbusRegisterAdapter
    {
        void WriteDefaults(IReadOnlyDictionary<string, object> defaults);
        void WritePoints(IReadOnlyDictionary<string, object> values, byte slaveId = 1);
        Dictionary<string, object> ReadAllControlRaw(IReadOnlyList<string> paramNames);
        object? ReadParsedPoint(string paramName, byte slaveId = 1);
    }
}

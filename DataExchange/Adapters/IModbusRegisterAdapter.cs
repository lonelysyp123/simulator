namespace EssSimulator.DataExchange.Adapters
{
    public interface IModbusRegisterAdapter
    {
        void WriteDefaults(IReadOnlyDictionary<string, object> defaults);
        void WritePoints(IReadOnlyDictionary<string, object> values, byte slaveId = 1, bool applyScale = true);
        Dictionary<string, object> ReadAllControlRaw(IReadOnlyList<string> paramNames);
        object? ReadParsedPoint(string paramName, byte slaveId = 1);
    }
}

namespace EssSimulator.Protocol.Modbus
{
    /// <summary>Modbus 与仿真之间的数据同步后端（ModbusDataSync 或 DataExchangeSession）。</summary>
    public interface IModbusSyncBackend
    {
        void Start();
        void Stop();
        void SetDataObjectByMesurePointName(string name, object value);
        void PublishControlToSlave(string name, object value);
        void InvalidateDataShadow(string name);
        object? GetDataObjectByMesurePointName(string name, IModbusSlave slave, ModbusParser parser);
    }
}

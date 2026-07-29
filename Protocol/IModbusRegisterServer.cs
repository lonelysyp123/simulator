namespace EssSimulator
{
    /// <summary>
    /// Modbus 寄存器读写门面（仿真设备与 LC 转发服务共用）。
    /// </summary>
    public interface IModbusRegisterServer
    {
        bool IsOnline { get; }
        IReadOnlyList<MapEntry> DataMaps { get; }
        IReadOnlyList<MapEntry> ControlMaps { get; }
        IReadOnlyList<MapEntry> RackControlMaps { get; }
        void SetDataObjectByMesurePointName(string name, object value);
        bool TrySetRackControl(int rackIndex, string name, object value, out string message);
        void SetDataStoreByMesurePointName(string name, object value);
        object? GetDataObjectByMesurePointName(string name);
        void PublishControlToSlave(string name, object value);
    }
}

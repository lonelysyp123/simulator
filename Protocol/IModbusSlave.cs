namespace IEC61850_simulatorServer2
{
    /// <summary>
    /// 统一 Modbus 从站对外接口（普通与BMS）
    /// </summary>
    public interface IModbusSlave
    {
        void DeviceConnect();
        void DeviceDisconnect();
        bool GetCommunicatorState();

        // 读取当前从站的所有数据（参数名->原始字节或值）
        Dictionary<string, object>? Read(byte slaveId = 1);

        // 读取当前从站的数据（参数名->原始字节或值）
        byte[] Read(string paramName);

        // 写入数据到从站（可选由具体实现决定目标单元/从站ID）
        bool Write(System.Collections.Generic.Dictionary<string, object> data, byte slaveId = 1);
    }
}
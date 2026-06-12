namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>可写点位的控制语义。</summary>
    public enum ControlSemantics
    {
        /// <summary>保持型：Modbus 值与仿真目标保持一致。</summary>
        Hold,

        /// <summary>脉冲型：检测到有效写入后触发一次，随后仿真与 Modbus 归零。</summary>
        Pulse,

        /// <summary>边沿型：值变化时触发副作用（如启停线圈），可配合仿真回写 Modbus。</summary>
        Edge
    }
}

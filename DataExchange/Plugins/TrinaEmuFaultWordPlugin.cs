using EssSimulator.DataExchange.Adapters;

namespace EssSimulator.DataExchange.Plugins
{
    /// <summary>
    /// TRINA 中压协议 EMU 故障/警告字组字插件。
    /// 协议位定义来自特定 PCS 厂家，与仿真模型 PcsData 的故障布尔量语义不同且不完全覆盖；
    /// 本插件维护「协议位 → 仿真故障」映射清单（即点位所需故障与仿真存在故障的交集），
    /// 按位 OR 组合成寄存器值，仿真不支持的位恒为 0。
    /// 点表标记格式：model=plugin|arg1=&lt;字键&gt;|arg2=&lt;设备根路径&gt;（如 emu1.PcsList[0]）。
    /// </summary>
    public sealed class TrinaEmuFaultWordPlugin : ITelemetryPlugin
    {
        /// <summary>字键 → 位映射表：(位号, 仿真故障属性名列表，任一为 true 即置位)。</summary>
        private static readonly Dictionary<string, IReadOnlyList<(int Bit, string[] Props)>> WordMaps =
            new(StringComparer.Ordinal)
            {
                // 模块警告字1（16 位）
                ["ModuleWarningWord1"] = new[]
                {
                    (0,  new[] { "InsulationAlarm" }),            // Bit0 总线1绝缘低（仿真无总线1/2之分，两位同源）
                    (1,  new[] { "InsulationAlarm" }),            // Bit1 总线2绝缘低
                    // Bit2 湿度过高：仿真无对应故障
                    // Bit3 除湿机故障：仿真无对应故障
                    // Bit4 UPS电池电压低：仿真无对应故障
                    (5,  new[] { "InternalOverTemp" }),           // Bit5 J板过温 → 机内过温
                    (6,  new[] { "DriveFault" }),                 // Bit6 相R驱动线松动 → 驱动故障
                    (7,  new[] { "DriveFault" }),                 // Bit7 相S驱动线松动 → 驱动故障
                    (8,  new[] { "DriveFault" }),                 // Bit8 相T驱动线松动 → 驱动故障
                    // Bit9 UPS交流电源丢失：仿真无对应故障
                    (10, new[] { "DcSurgeProtectorAbnormal" }),   // Bit10 DC SPD → 直流防雷器异常
                    // Bit11 辅助变压器SW馈电：仿真无对应故障
                    (12, new[] { "AcSurgeProtectorAbnormal" }),   // Bit12 AC SPD → 交流防雷器异常
                    // Bit13 辅助SPD：仿真无对应故障
                    // Bit14 3352通信丢失：仿真无对应故障
                    // Bit15 IMD通信丢失：仿真无对应故障
                },
                // 模块警告字2（5 位）
                ["ModuleWarningWord2"] = new[]
                {
                    (0, new[] { "InverterSoftwareOverCurrent" }), // Bit0 CBC警告R → 逆变软件过流
                    (1, new[] { "InverterSoftwareOverCurrent" }), // Bit1 CBC警告S
                    (2, new[] { "InverterSoftwareOverCurrent" }), // Bit2 CBC警告T
                    // Bit3 电池欠压保护：仿真无对应布尔量
                    (4, new[] { "AcFanAbnormal" }),               // Bit4 外部风扇欠流保护 → 交流风机异常
                }
            };

        public bool CanHandle(string wordKey) => WordMaps.ContainsKey(wordKey);

        public object? Compute(string wordKey, string deviceRoot, ISimulationDataAdapter simulation)
        {
            if (!WordMaps.TryGetValue(wordKey, out var map))
                return null;

            int word = 0;
            foreach (var (bit, props) in map)
            {
                foreach (var prop in props)
                {
                    if (ReadBool(simulation, $"{deviceRoot}.{prop}"))
                    {
                        word |= 1 << bit;
                        break;
                    }
                }
            }

            return word;
        }

        /// <summary>读取仿真布尔量；路径不存在/类型不符/异常均视为 false（协议位输出 0）。</summary>
        private static bool ReadBool(ISimulationDataAdapter simulation, string path)
        {
            try
            {
                return simulation.Read(path) switch
                {
                    bool b => b,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }
    }
}

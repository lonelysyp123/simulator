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
        /// <summary>系统故障总字键：遍历所有机组全部 PCS 模块，任一告警汇总非零即输出 1。</summary>
        public const string SystemFaultSummaryKey = "SystemFaultSummary";

        /// <summary>系统总状态（精简版）字键：聚合全部 PCS 模块运行状态，输出 1停机/3运行中/5故障/6告警。</summary>
        public const string SystemRunStateSummaryKey = "SystemRunStateSummary";

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

        public bool CanHandle(string wordKey) =>
            wordKey == SystemFaultSummaryKey || wordKey == SystemRunStateSummaryKey || WordMaps.ContainsKey(wordKey);

        public object? Compute(string wordKey, string deviceRoot, ISimulationDataAdapter simulation)
        {
            if (wordKey == SystemFaultSummaryKey)
                return ComputeSystemFaultSummary(deviceRoot, simulation);
            if (wordKey == SystemRunStateSummaryKey)
                return ComputeSystemRunStateSummary(deviceRoot, simulation);

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

        /// <summary>
        /// 系统故障总：deviceRoot 形如 emu1，去掉尾部编号得到机组前缀，
        /// 自 1 起逐台探测机组，对每台机组 PcsList 中全部模块读取
        /// AlarmSummary1/AlarmSummary2，任一非零即判故障（1），否则 0。
        /// </summary>
        private static int ComputeSystemFaultSummary(string deviceRoot, ISimulationDataAdapter simulation)
        {
            string prefix = (deviceRoot ?? string.Empty).TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (prefix.Length == 0 || prefix == deviceRoot)
                return 0;

            for (int unit = 1; unit <= 32; unit++)
            {
                string unitRoot = $"{prefix}{unit}";
                if (!TryReadInt(simulation, $"{unitRoot}.PcsList.Count", out int moduleCount))
                    break; // 首个缺失机组即停止探测（机组编号连续）

                for (int m = 0; m < moduleCount; m++)
                {
                    if (TryReadInt(simulation, $"{unitRoot}.PcsList[{m}].AlarmSummary1", out int summary1) && summary1 != 0)
                        return 1;
                    if (TryReadInt(simulation, $"{unitRoot}.PcsList[{m}].AlarmSummary2", out int summary2) && summary2 != 0)
                        return 1;
                }
            }

            return 0;
        }

        /// <summary>
        /// 系统总状态（精简版）：逐台探测机组并遍历全部模块，读取 OperationStatus 与
        /// AlarmSummary1/2。优先级：任一模块故障(OperationStatus=6)→5；否则任一告警
        /// 汇总非零→6；否则任一运行中(待机/充电/放电)→3；全停机→1。
        /// 仿真无启动暂态，不输出 2；待机按运行中处理。
        /// </summary>
        private static int ComputeSystemRunStateSummary(string deviceRoot, ISimulationDataAdapter simulation)
        {
            string prefix = (deviceRoot ?? string.Empty).TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (prefix.Length == 0 || prefix == deviceRoot)
                return 1;

            bool anyFault = false, anyAlarm = false, anyRunning = false;
            for (int unit = 1; unit <= 32; unit++)
            {
                string unitRoot = $"{prefix}{unit}";
                if (!TryReadInt(simulation, $"{unitRoot}.PcsList.Count", out int moduleCount))
                    break; // 首个缺失机组即停止探测（机组编号连续）

                for (int m = 0; m < moduleCount; m++)
                {
                    string moduleRoot = $"{unitRoot}.PcsList[{m}]";
                    if (TryReadInt(simulation, $"{moduleRoot}.OperationStatus", out int status))
                    {
                        if (status == 6)
                            anyFault = true;
                        else if (status is 2 or 4 or 5)
                            anyRunning = true;
                    }

                    if ((TryReadInt(simulation, $"{moduleRoot}.AlarmSummary1", out int summary1) && summary1 != 0)
                        || (TryReadInt(simulation, $"{moduleRoot}.AlarmSummary2", out int summary2) && summary2 != 0))
                        anyAlarm = true;
                }
            }

            return anyFault ? 5 : anyAlarm ? 6 : anyRunning ? 3 : 1;
        }

        /// <summary>读取整型遥测量；路径不存在/类型不符/异常均返回 false。</summary>
        private static bool TryReadInt(ISimulationDataAdapter simulation, string path, out int value)
        {
            value = 0;
            try
            {
                object? raw = simulation.Read(path);
                if (raw is not IConvertible convertible)
                    return false;
                value = Convert.ToInt32(convertible);
                return true;
            }
            catch
            {
                return false;
            }
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

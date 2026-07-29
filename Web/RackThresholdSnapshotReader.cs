using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Web
{
    public sealed class RackThresholdPointDto
    {
        public string ParamName { get; set; } = "";
        public int Address { get; set; }
        public int Scale { get; set; }
        public string Type { get; set; } = "u16";
        public string Description { get; set; } = "";
        public string PropertyName { get; set; } = "";
        /// <summary>1=一级故障 / 2=二级告警 / 3=三级保护（与点表中文命名一致）。</summary>
        public int Level { get; set; }
        public bool IsRecovery { get; set; }
        public string Category { get; set; } = "其他";
        public string UnitHint { get; set; } = "";
        /// <summary>当前工程值（模型值，= 寄存器原始值 / Scale）。</summary>
        public double? EngineeringValue { get; set; }
        /// <summary>当前寄存器原始值（工程值 × Scale，取整）。</summary>
        public int? RawValue { get; set; }
    }

    public sealed class RackThresholdSnapshotDto
    {
        public int UnitNumber { get; set; }
        public string Device { get; set; } = "";
        public int RackIndex { get; set; }
        public int ClusterCount { get; set; }
        public List<RackThresholdPointDto> Points { get; set; } = new();
    }

    /// <summary>BMS 簇告警门限快照：点表元数据 + 模型当前工程值。</summary>
    public static class RackThresholdSnapshotReader
    {
        public static RackThresholdSnapshotDto? Read(int unitNumber1Based, int rackIndex0)
        {
            string device = $"simBms{unitNumber1Based}";
            var store = SimulatorHost.Instance;
            if (!store.Contains(device))
                return null;

            var srv = store.Get<ModbusSimServer>(device);
            if (srv == null)
                return null;

            int clusterCount = ResolveClusterCount(unitNumber1Based);
            if (rackIndex0 < 0 || rackIndex0 >= clusterCount)
                rackIndex0 = 0;

            var points = new List<RackThresholdPointDto>();
            foreach (var entry in srv.RackControlMaps)
            {
                if (string.IsNullOrWhiteSpace(entry.ParamName))
                    continue;

                var model = ModbusSimServer.GetModelParam(entry.ModelSim ?? "");
                string arg1 = model?.Arg1 ?? "";
                if (string.IsNullOrWhiteSpace(arg1) ||
                    !arg1.Contains("Thresholds.", StringComparison.OrdinalIgnoreCase))
                    continue;

                string propertyName = ExtractPropertyName(arg1);
                string desc = entry.Description ?? entry.ParamName!;
                int scale = entry.Scale <= 0 ? 1 : entry.Scale;

                string path = ResolveModelPath(arg1, unitNumber1Based, rackIndex0);
                double? eng = null;
                int? raw = null;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    try
                    {
                        var o = SimServer.GetExtIfVariableVal(path);
                        if (o != null)
                        {
                            eng = Convert.ToDouble(o);
                            raw = (int)Math.Round(eng.Value * scale);
                        }
                    }
                    catch
                    {
                        /* 路径解析失败时留空，仍返回元数据供编辑 */
                    }
                }

                points.Add(new RackThresholdPointDto
                {
                    ParamName = entry.ParamName!,
                    Address = entry.Address,
                    Scale = scale,
                    Type = entry.Type ?? "u16",
                    Description = desc,
                    PropertyName = propertyName,
                    Level = DetectLevel(desc, propertyName),
                    IsRecovery = desc.Contains("恢复", StringComparison.Ordinal) ||
                                 propertyName.Contains("Recovery", StringComparison.OrdinalIgnoreCase),
                    Category = DetectCategory(desc, propertyName),
                    UnitHint = DetectUnitHint(desc, propertyName, scale),
                    EngineeringValue = eng,
                    RawValue = raw
                });
            }

            return new RackThresholdSnapshotDto
            {
                UnitNumber = unitNumber1Based,
                Device = device,
                RackIndex = rackIndex0,
                ClusterCount = clusterCount,
                Points = points
            };
        }

        private static int ResolveClusterCount(int unitNumber1Based)
        {
            try
            {
                var v = SimServer.GetExtIfVariableVal($"bms{unitNumber1Based}.BatteryStacks[0].Cluseter.Count");
                if (v is int i) return Math.Max(1, i);
                if (v is long l) return (int)Math.Max(1, Math.Min(int.MaxValue, l));
                if (v != null && int.TryParse(v.ToString(), out int p)) return Math.Max(1, p);
            }
            catch
            {
                /* fall through */
            }

            return GuiSimDataAccess.GetClusterCount();
        }

        private static string ResolveModelPath(string arg1, int unitNumber1Based, int rackIndex0)
        {
            // arg1: bmsdeviceId.BatteryStacks[0].Cluseter[rackId].Thresholds.Xxx
            return arg1
                .Replace("bmsdeviceId", $"bms{unitNumber1Based}", StringComparison.OrdinalIgnoreCase)
                .Replace("[rackId]", $"[{rackIndex0}]", StringComparison.OrdinalIgnoreCase)
                .Replace("rackId", rackIndex0.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractPropertyName(string arg1)
        {
            int idx = arg1.LastIndexOf('.');
            return idx >= 0 && idx < arg1.Length - 1 ? arg1[(idx + 1)..] : arg1;
        }

        private static int DetectLevel(string desc, string propertyName)
        {
            if (desc.Contains("一级", StringComparison.Ordinal) || propertyName.EndsWith("3", StringComparison.Ordinal))
                return 1;
            if (desc.Contains("二级", StringComparison.Ordinal) || propertyName.EndsWith("2", StringComparison.Ordinal))
                return 2;
            if (desc.Contains("三级", StringComparison.Ordinal) || propertyName.EndsWith("1", StringComparison.Ordinal))
                return 3;
            return 0;
        }

        private static string DetectCategory(string desc, string propertyName)
        {
            if (ContainsAny(desc, propertyName, "单体过压", "CellOvervoltage")) return "单体过压";
            if (ContainsAny(desc, propertyName, "单体欠压", "CellUndervoltage")) return "单体欠压";
            if (ContainsAny(desc, propertyName, "总电压过压", "Overvoltage") &&
                !ContainsAny(desc, propertyName, "单体", "Cell")) return "总压过压";
            if (ContainsAny(desc, propertyName, "总电压欠压", "Undervoltage") &&
                !ContainsAny(desc, propertyName, "单体", "Cell")) return "总压欠压";
            if (ContainsAny(desc, propertyName, "充电过流", "ChargeOvercurrent")) return "充电过流";
            if (ContainsAny(desc, propertyName, "放电过流", "DischargeOvercurrent")) return "放电过流";
            if (ContainsAny(desc, propertyName, "充电温度过高", "ChargeHighTemp")) return "充电高温";
            if (ContainsAny(desc, propertyName, "充电温度过低", "ChargeLowTemp")) return "充电低温";
            if (ContainsAny(desc, propertyName, "放电过温", "DischargeHighTemp")) return "放电高温";
            if (ContainsAny(desc, propertyName, "放电欠温", "DischargeLowTemp")) return "放电低温";
            if (ContainsAny(desc, propertyName, "SOC", "LowSOC")) return "低SOC";
            if (ContainsAny(desc, propertyName, "极柱", "PoleHighTemp")) return "极柱温度";
            if (ContainsAny(desc, propertyName, "绝缘", "Insulation")) return "绝缘";
            if (ContainsAny(desc, propertyName, "单体压差", "CellVoltageDifference")) return "单体压差";
            if (ContainsAny(desc, propertyName, "总电压压差", "TotalVoltageDifference")) return "总压压差";
            if (ContainsAny(desc, propertyName, "温差", "CellTempDifference")) return "温差";
            if (ContainsAny(desc, propertyName, "HVB", "高压箱")) return "高压箱温度";
            return "其他";
        }

        private static string DetectUnitHint(string desc, string propertyName, int scale)
        {
            if (ContainsAny(desc, propertyName, "SOC", "LowSOC")) return "pu(0~1)";
            if (ContainsAny(desc, propertyName, "温度", "Temp", "温差")) return "°C";
            if (ContainsAny(desc, propertyName, "过流", "Current")) return "A";
            if (ContainsAny(desc, propertyName, "绝缘", "Insulation")) return "kΩ";
            if (ContainsAny(desc, propertyName, "压差", "Difference") &&
                ContainsAny(desc, propertyName, "总电压", "Total")) return "V";
            if (ContainsAny(desc, propertyName, "压差", "VoltageDifference")) return "V";
            if (ContainsAny(desc, propertyName, "电压", "Voltage", "过压", "欠压")) return "V";
            return scale > 1 ? $"×1/{scale}" : "";
        }

        private static bool ContainsAny(string desc, string propertyName, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (desc.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                    propertyName.Contains(key, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

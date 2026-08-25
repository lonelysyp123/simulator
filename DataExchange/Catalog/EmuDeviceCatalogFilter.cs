using System.Text.RegularExpressions;
using EssSimulator.Configuration;

namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>
    /// EMU 点表绑定按机组构成分门控：EMU 点表 ModelSim 绑定只能指向机组内实际存在的设备模型
    /// （PCS / 电表 / 断路器）。找不到对应设备的绑定在目录编译期剔除，
    /// 点位保持未绑定语义（寄存器仅维持默认值，遥测不刷新、控制写入不生效）。
    /// </summary>
    public sealed partial class EmuDeviceCatalogFilter
    {
        private static readonly Regex EmuRootPattern = EmuRootRegex();

        [GeneratedRegex(@"^emu(\d+)$", RegexOptions.IgnoreCase)]
        private static partial Regex EmuRootRegex();

        private readonly IReadOnlyList<EssUnitConfig> _units;

        private EmuDeviceCatalogFilter(IReadOnlyList<EssUnitConfig> units) => _units = units;

        /// <summary>
        /// 为指定设备创建门控过滤器。仅 EMU 同构设备（simEmu / simLc）且传入了机组构成时启用；
        /// 无拓扑 overlay 的 legacy 配置（机组列表为空）不门控，保持现状。
        /// </summary>
        public static EmuDeviceCatalogFilter? Create(string serverName, IReadOnlyList<EssUnitConfig>? units)
        {
            if (!DataExchangeSession.IsEmuLikeDevice(serverName) || units is not { Count: > 0 })
                return null;
            return new EmuDeviceCatalogFilter(units);
        }

        /// <summary>
        /// 判断单条模型绑定路径是否指向机组内实际存在的设备。
        /// 路径根须为 emuK 机组根路径；PcsList[i] 要求 i 小于该机组 PCS 台数；
        /// ElectricityMeter 要求该机组绑定电表；Emu.PowerOnOff（单元高压断路器开合）要求该机组绑定断路器，
        /// 其余 Emu.* 为单元虚拟模型，恒允许。
        /// </summary>
        public bool Allows(string? bindingPath)
        {
            var target = DataTarget.ParseBindingPath(bindingPath);
            if (target == null)
                return false;

            var match = EmuRootPattern.Match(target.RootKey);
            if (!match.Success)
                return false;

            int unitIndex = int.Parse(match.Groups[1].Value) - 1;
            if (unitIndex < 0 || unitIndex >= _units.Count)
                return false;

            var unit = _units[unitIndex];
            string path = target.PropertyPath;

            if (path.StartsWith("PcsList[", StringComparison.OrdinalIgnoreCase))
            {
                int close = path.IndexOf(']', 8);
                if (close <= 8)
                    return false;
                return int.TryParse(path[8..close], out int pcsIndex)
                    && pcsIndex >= 0 && pcsIndex < unit.PcsCount;
            }

            if (path.StartsWith("ElectricityMeter", StringComparison.OrdinalIgnoreCase))
                return unit.HasUnitMeter;

            if (path.StartsWith("Emu.PowerOnOff", StringComparison.OrdinalIgnoreCase))
                return unit.HasUnitBreaker;

            // 其余路径（Emu.* 单元虚拟模型、BMS/直流母线等）恒允许
            return true;
        }

        /// <summary>
        /// 过滤 sum/max 点位的多条路径；过滤后少于两条（sum/max 非法绑定）时返回 null，整点剔除。
        /// </summary>
        public IReadOnlyList<string>? FilterPaths(IReadOnlyList<string> paths)
        {
            var allowed = new List<string>(paths.Count);
            foreach (var path in paths)
            {
                if (Allows(path))
                    allowed.Add(path);
            }
            return allowed.Count >= 2 ? allowed : null;
        }
    }
}

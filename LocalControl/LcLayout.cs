using EssSimulator.Configuration;

namespace EssSimulator.LocalControl
{
    /// <summary>LC 按储能单元组数展开：无 <c>emu_group</c> 时视为 1 个隐式组。</summary>
    internal static class LcLayout
    {
        /// <summary>标准片段每个组的直流通道槽位数（pcs1–pcs4）。</summary>
        public const int TemplateSlotsPerGroup = 4;

        /// <summary>LC 片段按组展开的安全上限：组 ≥21 时单元遥测与组遥测、组遥控与 mv_param1 地址冲突。</summary>
        public const int MaxGroupCount = 20;

        /// <summary>互斥 EMU 直控表只覆盖 2 路 PCS；台数更多时回退拼装片段。</summary>
        public const int ExclusiveEmuPcsLimit = 2;

        public static int GroupCount(EssUnitConfig? unit) =>
            unit is { HasGroups: true } ? Math.Max(1, unit.Groups.Count) : 1;

        public static int GroupCountForUnit(SimulatorConfig cfg, int unitIndex)
        {
            var devices = cfg.Devices;
            if (devices == null || unitIndex < 0 || unitIndex >= devices.Count)
                return 1;
            return GroupCount(devices[unitIndex]);
        }

        /// <summary>组内实际 PCS 台数；超出模板槽位的通道保持默认 0。</summary>
        public static int PcsCountInGroup(EssUnitConfig? unit, int groupIndexZeroBased)
        {
            if (unit is { HasGroups: true })
            {
                if (groupIndexZeroBased < 0 || groupIndexZeroBased >= unit.Groups.Count)
                    return 0;
                return unit.Groups[groupIndexZeroBased].PcsCount;
            }

            // 扁平或无 Devices：与 emu/standard 两槽直控表对齐
            return groupIndexZeroBased == 0 ? (unit?.PcsCount ?? 2) : 0;
        }

        /// <summary>本单元各组中最大 PCS 台数；无构成时为 0。</summary>
        public static int MaxPcsInAnyGroup(EssUnitConfig? unit)
        {
            if (unit == null)
                return 0;
            int groups = GroupCount(unit);
            int max = 0;
            for (int i = 0; i < groups; i++)
                max = Math.Max(max, PcsCountInGroup(unit, i));
            return max;
        }

        /// <summary>未知台数（0）或不超过 2 台时可用互斥 EMU 直控表。</summary>
        public static bool ShouldUseExclusiveEmuMap(int unitPcsCount) =>
            unitPcsCount <= ExclusiveEmuPcsLimit;
    }
}

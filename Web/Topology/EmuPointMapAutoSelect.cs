using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 工程保存时按 PCS 总数自动选型 EMU 点表：
    /// 2 台 PCS → standard（标准 EMU 点表），4 台 → trina_5.5MW，8 台 → trina_10MW。
    /// 其余数量不调整现有选型。选型写入 device-models.json，随下次重启生效。
    /// </summary>
    public static class EmuPointMapAutoSelect
    {
        public const string EmuTypeId = "emu";

        /// <summary>PCS 总数 → EMU 点表型号 id；不匹配返回 null。</summary>
        public static string? ResolveModelId(int pcsCount) => pcsCount switch
        {
            2 => "standard",
            4 => "trina_5.5MW",
            8 => "trina_10MW",
            _ => null
        };

        /// <summary>统计工程中 PCS 节点数量。</summary>
        public static int CountPcs(TopologyProject project) =>
            project?.Nodes.Count(n => n.TemplateId == "pcs") ?? 0;

        /// <summary>
        /// 按工程 PCS 数量更新 EMU 选型（保留其他设备类型的既有选型）。
        /// 返回本次写入的型号 id；数量不匹配、型号目录缺失或与现有选型一致时返回 null（不改动）。
        /// </summary>
        public static string? ApplyForProject(TopologyProject project, string? rootOverride = null)
        {
            var modelId = ResolveModelId(CountPcs(project));
            if (modelId == null) return null;

            var root = rootOverride ?? DeviceModelRegistry.FindModelsRoot();
            if (root == null) return null;

            var modelDir = Path.Combine(root, DeviceModelRegistry.ModelsRelativeDir, EmuTypeId, modelId);
            if (!Directory.Exists(modelDir)) return null;

            var selection = DeviceModelRegistry.LoadSelection(rootOverride);
            if (selection.Selections.TryGetValue(EmuTypeId, out var current) &&
                string.Equals(current, modelId, StringComparison.OrdinalIgnoreCase))
                return null;

            selection.Selections[EmuTypeId] = modelId;
            DeviceModelRegistry.SaveSelection(selection, rootOverride);
            return modelId;
        }
    }
}

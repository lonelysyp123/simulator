using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 工程保存时按 PCS 总数自动选型 LC 点表：
    /// 2 台 PCS → standard（基础 LC），4 台 → trina_5.5MW，8 台 → trina_10MW。
    /// 其余数量不调整现有 LC 选型。选型写入 device-models.json，随下次重启生效。
    /// 不改 EMU 选型；若 emu 仍指向已迁走的中压型号，则迁到 lc 并把 emu 回 standard。
    /// </summary>
    public static class LcPointMapAutoSelect
    {
        public const string LcTypeId = "lc";
        public const string EmuTypeId = "emu";
        public const string EmuStandardModelId = "standard";

        private static readonly HashSet<string> MovedFromEmuModelIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "trina_5.5MW",
            "trina_10MW"
        };

        /// <summary>PCS 总数 → LC 点表型号 id；不匹配返回 null。</summary>
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
        /// 按工程 PCS 数量更新 LC 选型（保留其他设备类型的既有选型，含 emu）。
        /// 同时把过期的 emu=trina_* 迁到 lc。
        /// 返回本次写入的 LC 型号 id；无改动时返回 null。
        /// </summary>
        public static string? ApplyForProject(TopologyProject project, string? rootOverride = null)
        {
            var root = rootOverride ?? DeviceModelRegistry.FindModelsRoot();
            if (root == null) return null;

            var selection = DeviceModelRegistry.LoadSelection(rootOverride);
            bool changed = false;
            string? applied = null;

            if (TryMigrateStaleEmuSelection(selection))
            {
                changed = true;
                applied = selection.Selections[LcTypeId];
            }

            var modelId = ResolveModelId(CountPcs(project));
            if (modelId != null)
            {
                var modelDir = Path.Combine(root, DeviceModelRegistry.ModelsRelativeDir, LcTypeId, modelId);
                if (Directory.Exists(modelDir))
                {
                    if (!selection.Selections.TryGetValue(LcTypeId, out var current) ||
                        !string.Equals(current, modelId, StringComparison.OrdinalIgnoreCase))
                    {
                        selection.Selections[LcTypeId] = modelId;
                        changed = true;
                        applied = modelId;
                    }
                }
            }

            if (!changed)
                return null;

            DeviceModelRegistry.SaveSelection(selection, rootOverride);
            return applied;
        }

        /// <summary>
        /// 将已迁到 LC 的中压型号从 emu 选型挪到 lc，并把 emu 回 standard。
        /// </summary>
        internal static bool TryMigrateStaleEmuSelection(DeviceModelSelection selection)
        {
            if (!selection.Selections.TryGetValue(EmuTypeId, out var emuModel) ||
                string.IsNullOrWhiteSpace(emuModel) ||
                !MovedFromEmuModelIds.Contains(emuModel))
                return false;

            selection.Selections[LcTypeId] = emuModel;
            selection.Selections[EmuTypeId] = EmuStandardModelId;
            return true;
        }
    }
}

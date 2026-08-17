using System.Text.Json;

namespace EssSimulator.Web.Topology
{
    /// <summary>启动时加载组态 overlay：工程模式开启且内容合法才生效。</summary>
    public static class TopologyOverlayLoader
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static bool IsUsable(TopologyRuntimeOverlay? overlay)
        {
            if (overlay?.EssUnits is not { Count: > 0 })
                return false;

            foreach (var unit in overlay.EssUnits)
            {
                if (unit == null)
                    return false;
                if (unit.Pcs is not { Count: > 0 })
                    return false;
                if (unit.Bms is not { Count: > 0 })
                    return false;
            }

            return true;
        }

        public static TopologyRuntimeOverlay? TryLoad(string contentRoot)
        {
            try
            {
                var modePath = Path.Combine(contentRoot, "configs", "topology", "runtime-mode.json");
                var overlayPath = Path.Combine(contentRoot, "configs", "topology", "generated", "runtime-overlay.json");
                if (!File.Exists(modePath) || !File.Exists(overlayPath))
                    return null;

                var mode = JsonSerializer.Deserialize<TopologyRuntimeMode>(File.ReadAllText(modePath), JsonOpts);
                if (mode?.EngineeringMode != true)
                    return null;

                var overlay = JsonSerializer.Deserialize<TopologyRuntimeOverlay>(File.ReadAllText(overlayPath), JsonOpts);
                if (!IsUsable(overlay))
                {
                    Console.WriteLine("[Topology] 工程 overlay 无效（缺少储能单元或 PCS/BMS），已忽略，改用 appsettings");
                    return null;
                }

                return overlay;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Topology] 加载工程 overlay 失败：{ex.Message}");
                return null;
            }
        }
    }
}

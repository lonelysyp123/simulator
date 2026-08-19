using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Web.Topology
{
    /// <summary>系统运行模式：appsettings 默认拓扑，或组态工程驱动。</summary>
    public sealed class TopologyRuntimeMode
    {
        public bool EngineeringMode { get; set; }
        public string? ActiveProjectId { get; set; }
        public string? ActiveProjectName { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class TopologyProjectSummary
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime UpdatedAtUtc { get; set; }
        public int NodeCount { get; set; }
        public int EmuCount { get; set; }
    }

    /// <summary>由组态工程生成、启动时覆盖 appsettings 对应节的运行时补丁。</summary>
    public sealed class TopologyRuntimeOverlay
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string SourceProjectId { get; set; } = "";
        public string SourceProjectName { get; set; } = "";
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public List<EssUnitConfig> EssUnits { get; set; } = new();
        public List<PvUnitRuntimeConfig> PvUnits { get; set; } = new();
        public PccConfig? Pcc { get; set; }
        public TransformerConfig? Transformer { get; set; }
        public UnitTransformerConfig? UnitTransformer { get; set; }
        public MeterConfig? Meter { get; set; }
        public PcsPhysicalConfig? Pcs { get; set; }
        /// <summary>组态「负载」节点映射的站用负载计划（有功仅允许 ≤0）。</summary>
        public LoadConfig? Load { get; set; }
        public List<string> Notes { get; set; } = new();
    }

    public sealed class SystemConfigState
    {
        public bool EngineeringMode { get; set; }
        public string? ActiveProjectId { get; set; }
        public string? ActiveProjectName { get; set; }
        public bool OverlayPresent { get; set; }
        public string Source { get; set; } = "appsettings"; // appsettings | topology
        public int RuntimeUnitCount { get; set; }
        public List<TopologyProjectSummary> Projects { get; set; } = new();
        public TopologyRuntimeOverlay? OverlaySummary { get; set; }
    }

    public sealed class SystemApplyRequest
    {
        public bool EngineeringMode { get; set; }
        public string? ProjectId { get; set; }
        public bool ConfirmRestart { get; set; } = true;
    }

    public sealed class SystemApplyResponse
    {
        public bool Ok { get; set; }
        public string Message { get; set; } = "";
        public bool Restarting { get; set; }
        public TopologyRuntimeOverlay? Overlay { get; set; }
        public List<string> Details { get; set; } = new();
    }
}

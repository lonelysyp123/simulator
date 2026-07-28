namespace EssSimulator.Configuration
{
    /// <summary>产品档位（对应 appsettings.json: Simulator.Edition）。</summary>
    public class EditionConfig
    {
        public const string Section = "Simulator:Edition";

        /// <summary>Community / Commercial / Custom（大小写不敏感）。</summary>
        public string Name { get; set; } = "Commercial";

        /// <summary>是否锁定电站拓扑（社区版：不允许通过扩容配置突破规模）。</summary>
        public bool LockTopology { get; set; }

        /// <summary>允许的最大储能单元数；0 表示不限制。社区版典型为 2。</summary>
        public int MaxEssUnits { get; set; }

        /// <summary>是否允许白盒切片等高级功能。</summary>
        public bool AllowDroopSlices { get; set; } = true;

        public bool IsCommunity =>
            string.Equals(Name, "Community", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Name, "社区版", StringComparison.OrdinalIgnoreCase);
    }
}

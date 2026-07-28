namespace EssSimulator.Configuration
{
    /// <summary>授权配置（appsettings: Simulator.License）。</summary>
    public class LicenseConfig
    {
        public const string Section = "Simulator:License";

        /// <summary>是否必须校验 license.txt。社区版 false；商业版/定制版 true。</summary>
        public bool Required { get; set; }

        /// <summary>授权文件名（相对运行目录）。</summary>
        public string FileName { get; set; } = "license.txt";
    }
}

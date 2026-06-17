using System.Text.Json.Serialization;

namespace EssSimulator.Display
{
    public sealed class PerfTestSuiteFile
    {
        public List<PerfTestSuite> Suites { get; set; } = new();
    }

    public sealed class PerfTestSuite
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>drive = 发指令等主接线量变化；observe = 轮询监控后发触发指令</summary>
        public string Mode { get; set; } = "drive";
        public List<PerfDriveCase> Cases { get; set; } = new();
        public List<PerfObserveCase> ObserveCases { get; set; } = new();
    }

    public sealed class PerfDriveCase
    {
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public int TimeoutMs { get; set; } = 5000;
        public double Tolerance { get; set; } = 0.5;
        public int SettleMs { get; set; } = 300;
    }

    public sealed class PerfObserveCase
    {
        public string Name { get; set; } = string.Empty;
        public PerfWatchTarget Watch { get; set; } = new();
        public string Trigger { get; set; } = string.Empty;
        public int TimeoutMs { get; set; } = 5000;
        public double Tolerance { get; set; } = 0.5;
        public int SettleMs { get; set; } = 300;
    }

    public sealed class PerfWatchTarget
    {
        /// <summary>snapshot | dpc</summary>
        public string Type { get; set; } = "snapshot";
        public string Metric { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
    }

    public sealed class PerfTestResultRow
    {
        public string Suite { get; init; } = string.Empty;
        public string Case { get; init; } = string.Empty;
        public string Mode { get; init; } = string.Empty;
        public long CommandTicks { get; init; }
        public long? SnapshotTicks { get; init; }
        public long? UiFrameTicks { get; init; }
        public double? SnapshotMs { get; init; }
        public double? UiFrameMs { get; init; }
        public bool Success { get; init; }
        public string? Note { get; init; }

        public void Print()
        {
            string snap = SnapshotMs.HasValue ? $"{SnapshotMs:0.###} ms" : "—";
            string ui = UiFrameMs.HasValue ? $"{UiFrameMs:0.###} ms" : "—";
            string status = Success ? "OK" : "FAIL";
            Console.WriteLine($"  [{status}] {Case}");
            Console.WriteLine($"        指令时刻 → 快照可见: {snap}  |  界面帧刷新: {ui}");
            if (!string.IsNullOrWhiteSpace(Note))
                Console.WriteLine($"        备注: {Note}");
        }
    }
}

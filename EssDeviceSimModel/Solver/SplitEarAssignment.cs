namespace EssSimulator.EssDeviceSimModel.Solver
{
    /// <summary>双耳箱变下 PCS 通道归属（全局通道索引）。</summary>
    public sealed class SplitEarAssignment
    {
        public IReadOnlyList<int> LeftChannels { get; init; } = Array.Empty<int>();
        public IReadOnlyList<int> RightChannels { get; init; } = Array.Empty<int>();
    }
}

namespace EssSimulator.EssDeviceSimModel.Model
{
    /// <summary>负载时段计划：仅用日内刻度，按阶跃保持到下一刻度。</summary>
    public sealed class LoadWindow
    {
        public TimeSpan Start { get; set; }
        /// <summary>方向约定：+ 向电网送电，- 从电网取电。</summary>
        public double ActivePowerPlan { get; set; }
        public double ReactivePowerPlan { get; set; }
    }
}

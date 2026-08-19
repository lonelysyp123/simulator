namespace EssSimulator.EssDeviceSimModel.Pv
{
    /// <summary>
    /// 光伏单元：一台 35 kV/690 V 箱变下挂 16 台组串逆变器；
    /// 单台逆变器仍为 16 簇 × 30 块、320 kW / 690 V。
    /// </summary>
    public sealed class PvUnitConfig
    {
        public const int DefaultInverterCount = 16;

        public int InverterCount { get; init; } = DefaultInverterCount;
        public PvInverterConfig Inverter { get; init; } = new();
        public double UnitXfPrimaryV { get; init; } = 35000;
        public double UnitXfSecondaryV { get; init; } = 690;
        public double UnitXfRatedKva { get; init; } = DefaultInverterCount * 320;
    }
}

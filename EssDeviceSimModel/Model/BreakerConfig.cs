namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class BreakerBranchConfig
    {
        public double RatedVoltageKv { get; set; } = 220;
        public double RatedCurrentA { get; set; } = 55000;
        public double FaultThresholdA { get; set; } = 60000;
        public ThreePhaseConnection PrimaryConnection { get; set; } = ThreePhaseConnection.Star;
        public ThreePhaseConnection SecondaryConnection { get; set; } = ThreePhaseConnection.Star;
        public bool InitialClosed { get; set; } = true;
    }

    public sealed class BreakerConfig
    {
        public const string Section = "Breaker";

        public BreakerBranchConfig Main { get; set; } = new();
        public BreakerBranchConfig Unit { get; set; } = new() { RatedVoltageKv = 35, RatedCurrentA = 3000, FaultThresholdA = 3500 };
    }
}

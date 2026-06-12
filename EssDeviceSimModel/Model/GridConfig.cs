namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class GridConfig
    {
        public const string Section = "Pcc";

        public double NominalLineVoltageV { get; set; } = 220000;
        public ThreePhaseConnection Connection { get; set; } = ThreePhaseConnection.Star;
        public double NominalFrequencyHz { get; set; } = 50;
        public double ShortCircuitMva { get; set; } = 750;
        public double MaxVoltageShiftPercent { get; set; } = 5;
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;
    }
}

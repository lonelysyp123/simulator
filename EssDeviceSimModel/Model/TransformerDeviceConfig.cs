namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class TransformerDeviceConfig
    {
        public double RatedPowerKva { get; set; } = 31500;
        public double PrimaryNominalLineVoltageV { get; set; } = 220000;
        public double SecondaryNominalLineVoltageV { get; set; } = 35000;
        public ThreePhaseConnection PrimaryConnection { get; set; } = ThreePhaseConnection.Star;
        public ThreePhaseConnection SecondaryConnection { get; set; } = ThreePhaseConnection.Star;
        public double NoLoadLossKw { get; set; } = 0.05;
        public double LoadLossKw { get; set; } = 0.2;
        public double ImpedancePercent { get; set; } = 4;
        public double ReactiveVoltageInfluenceCoefficient { get; set; } = 1.0;
        public double NoLoadCurrentPercent { get; set; } = 2;
        public bool MagnetizingInrushEnabled { get; set; } = true;
        public double MagnetizingInrushDvDtThresholdPuPerSec { get; set; } = 0.8;
        public double MagnetizingInrushPeakExtraMultipleOfRatedPrimary { get; set; } = 4.0;
        public double MagnetizingInrushDecayTimeConstantSec { get; set; } = 0.45;
        public double MagnetizingInrushMaxExtraMultipleOfRatedPrimary { get; set; } = 12.0;
    }
}

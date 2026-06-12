namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class TransformerState
    {
        public double Power { get; set; }
        public double PrimaryVoltage { get; set; }
        public double SecondaryVoltage { get; set; }
        public double PrimaryCurrent { get; set; }
        public double SecondaryCurrent { get; set; }
        public double LoadRatio { get; set; }
        public double Efficiency { get; set; }
        public double Temperature { get; set; }
        public double IronLoss { get; set; }
        public double CopperLoss { get; set; }
        public double TotalLoss { get; set; }
        public double PowerFactor { get; set; }
        public DateTime Timestamp { get; set; }
        public double MagnetizingNoLoadCurrentSecondary { get; set; }
        public double MagnetizingInrushCurrentSecondary { get; set; }
        public double MagnetizingCurrentSecondary { get; set; }
        public double MagnetizingInrushCurrentPrimary { get; set; }
    }
}

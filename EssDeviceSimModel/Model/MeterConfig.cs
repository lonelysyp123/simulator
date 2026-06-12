namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class PtConfig
    {
        public double PrimaryLineVoltageV { get; set; } = 220000;
        public double SecondaryLineVoltageV { get; set; } = 100;
        public ThreePhaseConnection Connection { get; set; } = ThreePhaseConnection.Star;

        public double Ratio => SecondaryLineVoltageV > 1e-9
            ? PrimaryLineVoltageV / SecondaryLineVoltageV
            : 0;
    }

    public sealed class CtConfig
    {
        public double PrimaryCurrentA { get; set; } = 2000;
        public double SecondaryCurrentA { get; set; } = 5;

        public double Ratio => SecondaryCurrentA > 1e-9
            ? PrimaryCurrentA / SecondaryCurrentA
            : 0;
    }

    public sealed class MeterInstanceConfig
    {
        public string? MountDescription { get; set; }
        public PtConfig Pt { get; set; } = new();
        public CtConfig Ct { get; set; } = new();
        public MeterReportedQuantity ReportedQuantity { get; set; } = MeterReportedQuantity.Primary;
        public double BurdenVa { get; set; } = 5;
        public string AccuracyClass { get; set; } = "0.2S";
    }

    public sealed class MeterConfig
    {
        public const string Section = "Meter";

        public MeterInstanceConfig PccMeter { get; set; } = new();
    }
}

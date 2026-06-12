namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class DcSnapshot
    {
        public double VoltageV { get; init; }
        public double CurrentA { get; init; }

        public double PowerKw => VoltageV * CurrentA / 1000.0;

        public bool IsEnergized(double voltageThresholdV = 1.0) => VoltageV > voltageThresholdV;
    }
}

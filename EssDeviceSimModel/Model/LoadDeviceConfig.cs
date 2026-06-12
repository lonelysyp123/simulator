namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class LoadDeviceConfig
    {
        public double ActivePowerKw { get; set; }
        public double ReactivePowerKvar { get; set; }
        public ThreePhaseConnection Connection { get; set; } = ThreePhaseConnection.Star;
        public bool Powered { get; set; } = true;
    }
}

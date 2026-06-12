namespace EssSimulator.EssDeviceSimModel.Propagation
{
    public readonly record struct BusPowerContribution(double ActivePowerKw, double ReactivePowerKvar)
    {
        public static BusPowerContribution Zero => new(0, 0);
    }
}

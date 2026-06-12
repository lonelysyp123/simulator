namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class DeviceStepContext
    {
        public DateTime SimulationTime { get; init; } = DateTime.Now;
        public TimeSpan Step { get; init; }
        public bool MainBreakerClosed { get; init; } = true;
        public bool UtilityGridAvailable { get; init; } = true;
    }
}

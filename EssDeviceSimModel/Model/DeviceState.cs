namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class BreakerState
    {
        public bool IsClosed { get; set; } = true;
        public bool IsTripped { get; set; }
    }

    public sealed class DeviceFaultState
    {
        public ushort FaultCode { get; init; }
        public string? FaultMessage { get; init; }
        public bool HasFault => FaultCode != 0;
    }
}

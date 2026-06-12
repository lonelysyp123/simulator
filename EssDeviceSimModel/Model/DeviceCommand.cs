namespace EssSimulator.EssDeviceSimModel.Model
{
    public enum DeviceCommandKind
    {
        CloseBreaker,
        OpenBreaker,
        ResetBreakerTrip,
        PcsStartStop,
        PcsActivePower,
        PcsReactivePower,
        PcsIslandVoltage,
        PcsBlackStartEnable,
        DcLinkClose,
        DcLinkOpen
    }

    public sealed class DeviceCommand
    {
        public DeviceCommandKind Kind { get; init; }
        public double NumericValue { get; init; }
        public bool BoolValue { get; init; }
    }
}

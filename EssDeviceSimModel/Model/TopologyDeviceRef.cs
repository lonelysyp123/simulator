namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class TopologyDeviceRef
    {
        public string DeviceId { get; init; } = string.Empty;
        public ElectricalDeviceKind Kind { get; init; }
        public string? ConfigRef { get; init; }
        public string? AttachedBusId { get; init; }
        public int? UnitIndex { get; init; }
        public int? ChannelIndex { get; init; }
    }
}

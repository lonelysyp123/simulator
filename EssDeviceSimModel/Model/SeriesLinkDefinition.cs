namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class SeriesLinkDefinition
    {
        public string LinkId { get; init; } = string.Empty;
        public string DeviceId { get; init; } = string.Empty;
        public ElectricalDeviceKind DeviceKind { get; init; }
        public string UpstreamBusId { get; init; } = string.Empty;
        public string DownstreamBusId { get; init; } = string.Empty;
    }
}

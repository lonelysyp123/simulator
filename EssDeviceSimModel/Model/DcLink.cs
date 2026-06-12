namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class DcLink
    {
        public string LinkId { get; init; } = string.Empty;
        public string PcsDeviceId { get; init; } = string.Empty;
        public string BmsDeviceId { get; init; } = string.Empty;
        public bool DefaultClosed { get; init; } = true;
        public bool IsClosed { get; set; } = true;
    }
}

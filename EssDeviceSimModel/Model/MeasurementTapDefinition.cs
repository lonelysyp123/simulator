namespace EssSimulator.EssDeviceSimModel.Model
{
    public enum MeterReportedQuantity
    {
        Primary,
        Secondary
    }

    public sealed class MeasurementTapDefinition
    {
        public string MeterDeviceId { get; init; } = string.Empty;
        public string SourceDeviceId { get; init; } = string.Empty;
        public string SourcePortId { get; init; } = string.Empty;
        public MeterReportedQuantity Quantity { get; init; } = MeterReportedQuantity.Primary;
    }
}

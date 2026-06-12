namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class ElectricalBus
    {
        public string BusId { get; init; } = string.Empty;
        public double NominalLineVoltageV { get; init; }
        public ThreePhaseConnection Connection { get; init; } = ThreePhaseConnection.Star;
        public string? Description { get; init; }

        public AcInternalQuantities BusQuantity { get; set; } = new();
    }
}

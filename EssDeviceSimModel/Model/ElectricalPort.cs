namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class ElectricalPort
    {
        public string PortId { get; init; } = string.Empty;
        public PortKind Kind { get; init; } = PortKind.BusConnected;
        public ElectricalPortSnapshot Input { get; set; } = new() { Domain = ElectricalDomain.AcThreePhase };
        public ElectricalPortSnapshot Output { get; set; } = new() { Domain = ElectricalDomain.AcThreePhase };
    }
}

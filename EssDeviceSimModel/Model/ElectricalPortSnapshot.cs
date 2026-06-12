namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class ElectricalPortSnapshot
    {
        public ElectricalDomain Domain { get; init; }
        public AcPortSnapshot? Ac { get; init; }
        public DcSnapshot? Dc { get; init; }

        public static ElectricalPortSnapshot FromAc(AcInternalQuantities internalQty) =>
            new()
            {
                Domain = ElectricalDomain.AcThreePhase,
                Ac = new AcPortSnapshot { Internal = internalQty }
            };

        public static ElectricalPortSnapshot FromDc(DcSnapshot dc) =>
            new()
            {
                Domain = ElectricalDomain.Dc,
                Dc = dc
            };
    }
}

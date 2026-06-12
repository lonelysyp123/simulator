namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class ElectricalTopologyConfig
    {
        public const string Section = "ElectricalTopology";

        public int Version { get; set; } = 1;
        public ThreePhaseConnection DefaultAcConnection { get; set; } = ThreePhaseConnection.Star;
        public double DefaultFrequencyHz { get; set; } = 50.0;
        public IList<BusDefinition> Buses { get; set; } = new List<BusDefinition>();
        public IList<SeriesLinkDefinition> SeriesLinks { get; set; } = new List<SeriesLinkDefinition>();
        public IList<DcLinkDefinition> DcLinks { get; set; } = new List<DcLinkDefinition>();
        public IList<TopologyDeviceRef> Devices { get; set; } = new List<TopologyDeviceRef>();
        public IList<MeasurementTapDefinition> MeasurementTaps { get; set; } = new List<MeasurementTapDefinition>();
    }

    public sealed class BusDefinition
    {
        public string Id { get; set; } = string.Empty;
        public double NominalLineVoltageV { get; set; }
        public ThreePhaseConnection Connection { get; set; } = ThreePhaseConnection.Star;
        public string? Description { get; set; }
    }

    public sealed class DcLinkDefinition
    {
        public string LinkId { get; set; } = string.Empty;
        public string PcsDeviceId { get; set; } = string.Empty;
        public string BmsDeviceId { get; set; } = string.Empty;
        public bool DefaultClosed { get; set; } = true;
    }
}

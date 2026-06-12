namespace EssSimulator.EssDeviceSimModel.Model
{
    public sealed class NetworkTopology
    {
        public int Version { get; init; } = 1;
        public ThreePhaseConnection DefaultAcConnection { get; init; } = ThreePhaseConnection.Star;
        public double DefaultFrequencyHz { get; init; } = 50.0;

        public IList<ElectricalBus> Buses { get; init; } = new List<ElectricalBus>();
        public IList<SeriesLinkDefinition> SeriesLinks { get; init; } = new List<SeriesLinkDefinition>();
        public IList<DcLink> DcLinks { get; init; } = new List<DcLink>();
        public IList<TopologyDeviceRef> Devices { get; init; } = new List<TopologyDeviceRef>();
        public IList<MeasurementTapDefinition> MeasurementTaps { get; init; } = new List<MeasurementTapDefinition>();
    }
}

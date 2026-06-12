using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Model
{
    public static class ElectricalTopologyFactory
    {
        public static NetworkTopology FromConfig(ElectricalTopologyConfig config)
        {
            var topology = new NetworkTopology
            {
                Version = config.Version,
                DefaultAcConnection = config.DefaultAcConnection,
                DefaultFrequencyHz = config.DefaultFrequencyHz
            };

            foreach (var bus in config.Buses)
            {
                topology.Buses.Add(new ElectricalBus
                {
                    BusId = bus.Id,
                    NominalLineVoltageV = bus.NominalLineVoltageV,
                    Connection = bus.Connection,
                    Description = bus.Description,
                    BusQuantity = new AcInternalQuantities
                    {
                        Connection = bus.Connection,
                        LineVoltageV = bus.NominalLineVoltageV,
                        FrequencyHz = config.DefaultFrequencyHz
                    }
                });
            }

            foreach (var link in config.SeriesLinks)
                topology.SeriesLinks.Add(link);

            foreach (var dc in config.DcLinks)
            {
                topology.DcLinks.Add(new DcLink
                {
                    LinkId = dc.LinkId,
                    PcsDeviceId = dc.PcsDeviceId,
                    BmsDeviceId = dc.BmsDeviceId,
                    DefaultClosed = dc.DefaultClosed,
                    IsClosed = dc.DefaultClosed
                });
            }

            foreach (var device in config.Devices)
                topology.Devices.Add(device);

            foreach (var tap in config.MeasurementTaps)
                topology.MeasurementTaps.Add(tap);

            return topology;
        }
    }
}

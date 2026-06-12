using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Propagation;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    public sealed class GridSimulator : IGridDevice, ISelfActivatingElectricalSource
    {
        private readonly GridConfig _config;
        private double _aggregatedReactiveKvar;

        public GridSimulator(string deviceId, GridConfig config)
        {
            DeviceId = deviceId;
            _config = config;
            Port = CreatePort("grid", config.Connection);
        }

        public string DeviceId { get; }
        public ElectricalDeviceKind Kind => ElectricalDeviceKind.Grid;
        public ElectricalPort Port { get; }
        public ElectricalPort OutputPort => Port;
        public IReadOnlyList<ElectricalPort> Ports => new[] { Port };

        public void SetAggregatedReactivePowerKvar(double totalReactiveKvar) =>
            _aggregatedReactiveKvar = totalReactiveKvar;

        public void Activate(DeviceStepContext context, TimeSpan step) => Step(context, step);

        public void Step(DeviceStepContext context, TimeSpan step)
        {
            double lineVoltageV = GridFeedbackConventions.CalculatePccLineVoltage(
                _config.NominalLineVoltageV,
                _aggregatedReactiveKvar,
                _config.ShortCircuitMva,
                _config.ReactiveVoltageInfluenceCoefficient,
                _config.MaxVoltageShiftPercent);

            if (!context.MainBreakerClosed)
                lineVoltageV = 0;

            var internalQty = new AcInternalQuantities
            {
                Connection = _config.Connection,
                LineVoltageV = lineVoltageV,
                LineCurrentA = 0,
                PhaseAngleDeg = 0,
                FrequencyHz = lineVoltageV > 1.0 ? _config.NominalFrequencyHz : 0
            };

            AcPortHelper.WriteAcOutput(Port, internalQty);
        }

        private static ElectricalPort CreatePort(string portId, ThreePhaseConnection connection)
        {
            var empty = new AcInternalQuantities { Connection = connection };
            return new ElectricalPort
            {
                PortId = portId,
                Kind = PortKind.BusConnected,
                Input = ElectricalPortSnapshot.FromAc(empty),
                Output = ElectricalPortSnapshot.FromAc(empty)
            };
        }
    }
}

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    public sealed class BusVoltageChangedEventArgs
    {
        public required ElectricalBusNode SourceBus { get; init; }
        public required double LineVoltageV { get; init; }
        public required double FrequencyHz { get; init; }
        public required PropagationSweepContext Sweep { get; init; }
    }
}

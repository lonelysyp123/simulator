using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    public sealed class ElectricalOutputChangedEventArgs
    {
        public required IElectricalDevice Source { get; init; }
        public required ElectricalPort Port { get; init; }
        public required ElectricalPortSnapshot Output { get; init; }
        public required DeviceStepContext Context { get; init; }
        public required TimeSpan Step { get; init; }
    }
}

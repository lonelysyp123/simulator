using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IElectricalDevice
    {
        string DeviceId { get; }
        ElectricalDeviceKind Kind { get; }
        IReadOnlyList<ElectricalPort> Ports { get; }

        void Step(DeviceStepContext context, TimeSpan step);
    }
}

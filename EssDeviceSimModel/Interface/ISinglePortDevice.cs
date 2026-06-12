using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface ISinglePortDevice : IElectricalDevice
    {
        ElectricalPort Port { get; }
    }
}

using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface ITwoPortDevice : IElectricalDevice
    {
        ElectricalPort Primary { get; }
        ElectricalPort Secondary { get; }
    }
}

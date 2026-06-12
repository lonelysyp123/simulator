using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IAcDcConverterDevice : IElectricalDevice
    {
        ElectricalPort Ac { get; }
        ElectricalPort Dc { get; }
    }
}

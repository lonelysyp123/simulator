using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IPcsDevice : IAcDcConverterDevice, IControllableDevice, IFaultableDevice
    {
        bool GridAvailable { get; }
    }
}

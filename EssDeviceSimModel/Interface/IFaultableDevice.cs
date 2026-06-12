using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IFaultableDevice
    {
        DeviceFaultState Fault { get; }
    }
}

using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IBreakerDevice : ITwoPortDevice, IControllableDevice, IFaultableDevice
    {
        BreakerState SwitchState { get; }
    }
}

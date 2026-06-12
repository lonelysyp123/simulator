using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IControllableDevice
    {
        void ApplyCommand(DeviceCommand command);
    }
}

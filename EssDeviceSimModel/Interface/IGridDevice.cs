using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IGridDevice : ISinglePortDevice
    {
        void SetAggregatedReactivePowerKvar(double totalReactiveKvar);
    }
}

using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    public interface IBmsDevice : ISinglePortDevice, IFaultableDevice
    {
        bool IsLinked { get; set; }
        double Soc { get; }
    }
}

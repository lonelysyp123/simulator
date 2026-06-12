using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>向母线上报 P/Q 功率意图（叶子：负载、PCS 等）。</summary>
    public interface IBusPowerContributor
    {
        string ContributorId { get; }

        BusPowerContribution GetBusPowerContribution(DeviceStepContext context);
    }
}

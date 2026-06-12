namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>向电网反馈本站无功功率，供 Q-U 调压计算。</summary>
    public interface IReactivePowerContributor
    {
        double GetContributedReactivePowerKvar();
    }
}

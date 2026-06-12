using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 电压源等无需电气输入、由自身周期激活动作产生输出的设备。
    /// </summary>
    public interface ISelfActivatingElectricalSource : IElectricalDevice
    {
        ElectricalPort OutputPort { get; }

        /// <summary>计算并刷新输出端口，不依赖上游输入。</summary>
        void Activate(DeviceStepContext context, TimeSpan step);
    }
}

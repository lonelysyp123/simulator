using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 向指定母线注入电压的本地源（如黑启动 PCS V/f），与电网等全局源互补。
    /// </summary>
    public interface IBusVoltageSource
    {
        string SourceId { get; }

        /// <summary>当前周期是否应向所注册母线注入电压。</summary>
        bool IsInjecting(DeviceStepContext context);

        /// <summary>注入电压（线电压 V）与频率 Hz。</summary>
        (double LineVoltageV, double FrequencyHz) GetInjection(DeviceStepContext context);
    }
}

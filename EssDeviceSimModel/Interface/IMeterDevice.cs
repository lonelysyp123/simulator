using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Interface
{
    /// <summary>
    /// 带 CT/PT 变比的测量设备；不改变网络电气状态。
    /// </summary>
    public interface IMeterDevice : ISinglePortDevice
    {
        MeterInstanceConfig Config { get; }
        MeterTelemetry Telemetry { get; }

        void SampleFrom(AcInternalQuantities primaryQuantities, TimeSpan step);
    }
}

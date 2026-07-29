namespace EssSimulator.EssDeviceSimModel.Interface
{
    /// <summary>电气损耗源：供热网络注入热功率（W）。</summary>
    public interface IElectricalLossSource
    {
        /// <summary>最近一步（或当前状态）对应的欧姆/变换损耗（W）。</summary>
        double GetElectricalLossWatts();
    }

    /// <summary>可接受环境温度边界条件的设备。</summary>
    public interface ITemperatureAware
    {
        /// <summary>写入环境温度（°C），影响散热与本步热模型。</summary>
        void ApplyAmbientTemperature(double ambientCelsius);

        /// <summary>设备当前温度（°C）。</summary>
        double TemperatureCelsius { get; }
    }
}

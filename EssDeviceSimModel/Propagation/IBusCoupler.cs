namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 连接上下游母线的串联元件（断路器、变压器等）：上游电压变化时传播至下游。
    /// </summary>
    public interface IBusCoupler
    {
        string CouplerId { get; }
        ElectricalBusNode UpstreamBus { get; }
        ElectricalBusNode DownstreamBus { get; }

        /// <summary>注册到上游母线的电压变化通知。</summary>
        void Attach();
    }
}

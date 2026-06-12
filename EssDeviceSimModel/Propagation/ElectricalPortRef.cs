namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>电气端口唯一标识，用于注册/发布路由。</summary>
    public readonly record struct ElectricalPortRef(string DeviceId, string PortId)
    {
        public override string ToString() => $"{DeviceId}.{PortId}";
    }
}

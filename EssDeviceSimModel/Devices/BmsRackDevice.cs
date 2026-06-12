using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    /// <summary>
    /// BMS 设备：四层电池堆物理、阈值保护、DC 端口与 PCS 并网链路。
    /// 与 <see cref="ElectricalNetwork.BmsDevices"/> 共用实例。
    /// </summary>
    public sealed class BmsRackDevice : IBmsDevice
    {
        private readonly BatteryRackSimulator _rack;
        private DeviceFaultState _fault = new();

        public BmsRackDevice(string deviceId, BatteryRackSimulator rack)
        {
            DeviceId = deviceId;
            _rack = rack;
            Port = CreatePort();
            SyncPortFromRack();
            RefreshFaultState();
        }

        public string DeviceId { get; }
        public BatteryRackSimulator Rack => _rack;
        public ElectricalDeviceKind Kind => ElectricalDeviceKind.Bms;
        public DeviceFaultState Fault => _fault;
        public ElectricalPort Port { get; }
        public IReadOnlyList<ElectricalPort> Ports => new[] { Port };

        public bool IsLinked
        {
            get => _rack.GetRackState()?.IsPcsLinked ?? false;
            set
            {
                var state = _rack.GetRackState();
                if (state != null)
                    state.IsPcsLinked = value;
            }
        }

        public ushort FaultCode => _rack.GetRackState()?.IsFault ?? 0;

        public bool HasBlockingFault => FaultCode != 0;

        public double Soc
        {
            get
            {
                var state = _rack.GetRackState();
                if (state?.ClusterStates == null || state.ClusterStates.Count == 0)
                    return 0;
                return state.ClusterStates.Average(c => c.MinPackSOC);
            }
        }

        public void Step(DeviceStepContext context, TimeSpan step) => SyncPortFromRack();

        /// <summary>ESS 主循环：推进电芯/SOC 物理并刷新 DC 端口。</summary>
        public void UpdatePhysics(double rackCurrent, double ambientTemp, DateTime timeStamp, TimeSpan step)
        {
            _rack.Update(rackCurrent, ambientTemp, timeStamp, step);
            SyncPortFromRack();
        }

        /// <summary>并网链路写入（Rack 状态 SSOT）。</summary>
        public void SetPcsLinked(bool linked) => IsLinked = linked;

        /// <summary>将物理量映射到 BMS DTO、评估簇级阈值并回写 Rack 故障态。</summary>
        public void SyncTelemetryAndProtection(BatteryManagementSystemData bmsData)
        {
            var rackState = _rack.GetRackState();
            if (rackState == null || bmsData == null)
                return;

            BmsMapper.MapRackToStack(rackState, bmsData);
            BmsMapper.MapClusters(_rack, bmsData); // 遥测映射 + BmsRackProtection 阈值评估
            BmsMapper.SyncFaultToRack(bmsData, rackState);
            RefreshFaultState();
        }

        public void SyncPortFromRack()
        {
            var state = _rack.GetRackState();
            double voltageV = IsLinked ? state?.TotalVoltage ?? 0 : 0;
            double currentA = Port.Input.Dc?.CurrentA ?? state?.TotalCurrent ?? 0;

            AcPortHelper.WriteDcOutput(Port, new DcSnapshot
            {
                VoltageV = Math.Max(0, voltageV),
                CurrentA = currentA
            });
        }

        public void ApplyDcInputFromPcs(double currentA)
        {
            Port.Input = ElectricalPortSnapshot.FromDc(new DcSnapshot { CurrentA = currentA });
        }

        private void RefreshFaultState()
        {
            var state = _rack.GetRackState();
            _fault = new DeviceFaultState { FaultCode = state?.IsFault ?? 0 };
        }

        private static ElectricalPort CreatePort() =>
            new()
            {
                PortId = "dc",
                Kind = PortKind.DcLink,
                Input = ElectricalPortSnapshot.FromDc(new DcSnapshot()),
                Output = ElectricalPortSnapshot.FromDc(new DcSnapshot())
            };
    }
}

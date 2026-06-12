using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Solver
{
    /// <summary>
    /// 电气拓扑运行时容器：持有设备实例与求解器。
    /// </summary>
    public sealed class ElectricalNetwork
    {
        public NetworkTopology Topology { get; init; } = new();
        public GridSimulator Grid { get; init; } = null!;
        public BreakerSimulator MainBreaker { get; init; } = null!;
        public TransformerDevice MainTransformer { get; init; } = null!;
        public LoadDevice Load { get; init; } = null!;
        public MeterSimulator PccMeter { get; init; } = null!;
        public IReadOnlyList<BreakerSimulator> UnitBreakers { get; init; } = Array.Empty<BreakerSimulator>();
        public IReadOnlyList<TransformerDevice> UnitTransformers { get; init; } = Array.Empty<TransformerDevice>();
        public IReadOnlyList<PcsDevice> PcsDevices { get; init; } = Array.Empty<PcsDevice>();
        public IReadOnlyList<BmsRackDevice> BmsDevices { get; init; } = Array.Empty<BmsRackDevice>();
        public IReadOnlyList<DcLink> DcLinks { get; init; } = Array.Empty<DcLink>();

        public INetworkSolver Solver { get; set; } = null!;

        public double PccLineVoltageV { get; internal set; }
        public double StationBus35LineVoltageV { get; internal set; }

        public ElectricalBus? GetBus(string busId) =>
            Topology.Buses.FirstOrDefault(b => b.BusId == busId);
    }
}

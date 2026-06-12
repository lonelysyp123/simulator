using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Solver;

namespace EssSimulator.Tests.Solver;

public class NetworkControlBridgeTests
{
    [Fact]
    public void ApplyMainBreakerClosed_UpdatesNetworkAndLegacy()
    {
        var legacy = new Breaker { IsClosed = true };
        var load = new LoadDevice("load_35", 100, 0);
        var network = CreateMinimalNetwork(load);

        NetworkControlBridge.ApplyMainBreakerClosed(network, legacy, load, closed: false);

        Assert.False(NetworkControlBridge.IsBreakerClosed(network.MainBreaker));
        Assert.False(legacy.IsClosed);
        Assert.Equal(0, load.ActivePower);
        Assert.Equal(0, load.ReactivePower);
    }

    [Fact]
    public void ApplyUnitBreakerClosed_UpdatesNetworkAndLegacy()
    {
        var legacyUnits = new List<Breaker> { new() { IsClosed = true } };
        var load = new LoadDevice("load_35", 0, 0);
        var network = CreateMinimalNetwork(load);

        NetworkControlBridge.ApplyUnitBreakerClosed(network, legacyUnits, unitIndex: 0, closed: false);

        Assert.False(NetworkControlBridge.IsBreakerClosed(network.UnitBreakers[0]));
        Assert.False(legacyUnits[0].IsClosed);
    }

    [Fact]
    public void ApplyMainBreakerClosed_WhenTripped_DoesNotForceClose()
    {
        var legacy = new Breaker { IsClosed = true };
        var load = new LoadDevice("load_35", 0, 0);
        var network = CreateMinimalNetwork(load);
        network.MainBreaker.SwitchState.IsTripped = true;
        network.MainBreaker.SwitchState.IsClosed = false;

        NetworkControlBridge.ApplyMainBreakerClosed(network, legacy, load, closed: true);

        Assert.False(NetworkControlBridge.IsBreakerClosed(network.MainBreaker));
        Assert.False(legacy.IsClosed);
    }

    [Fact]
    public void SyncLoadPlan_RefreshesScheduleWhenPowered()
    {
        var load = new LoadDevice("load_35", 80, 10);
        var network = CreateMinimalNetwork(load);
        var simTime = new DateTime(2026, 6, 11, 12, 0, 0);

        NetworkControlBridge.SyncLoadPlan(network, load, simTime);

        Assert.NotEqual(0, load.ActivePower);
        Assert.Equal(10, load.ReactivePower);
    }

    private static ElectricalNetwork CreateMinimalNetwork(LoadDevice load) =>
        new()
        {
            Topology = new NetworkTopology(),
            Grid = new GridSimulator("grid", new GridConfig()),
            MainBreaker = new BreakerSimulator("main_breaker", new BreakerBranchConfig { InitialClosed = true }),
            MainTransformer = new TransformerDevice("main_transformer", new TransformerDeviceConfig()),
            Load = load,
            PccMeter = new MeterSimulator("pcc_meter", new MeterInstanceConfig()),
            UnitBreakers = new[] { new BreakerSimulator("unit_breaker_u0", new BreakerBranchConfig { InitialClosed = true }) },
            UnitTransformers = Array.Empty<TransformerDevice>(),
            PcsDevices = Array.Empty<PcsDevice>(),
            BmsDevices = Array.Empty<BmsRackDevice>(),
            DcLinks = Array.Empty<DcLink>(),
            Solver = null!
        };
}

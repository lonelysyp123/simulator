using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Propagation;
using EssSimulator.EssDeviceSimModel.Solver;

namespace EssSimulator.Tests.Propagation;

public class RadialNetworkGraphTests
{
    private static RadialNetworkGraph CreateGraph()
    {
        var simCfg = new SimulatorConfig
        {
            Devices = { new EssUnitConfig() }
        };
        var pcsCfg = new PcsPhysicalConfig();
        var pccCfg = new PccConfig();
        var network = NetworkTopologyBuilder.Build(
            simCfg,
            pcsCfg,
            new TransformerConfig(),
            new UnitTransformerConfig(),
            new LoadConfig(),
            pccCfg);
        return new RadialNetworkGraph(network, pccCfg, pcsCfg);
    }

    [Fact]
    public void BusIds_match_RuntimeBusIds()
    {
        var graph = CreateGraph();

        Assert.Equal(RuntimeBusIds.Grid, graph.BusGrid.BusId);
        Assert.Equal(RuntimeBusIds.AfterMainBreaker, graph.BusAfterMainBreaker.BusId);
        Assert.Equal(RuntimeBusIds.Station35, graph.Bus35.BusId);
        Assert.Equal(RuntimeBusIds.Unit690(0), graph.UnitBuses690[0].BusId);
    }

    [Fact]
    public void FindBus_AfterMainBreaker_returns_same_node()
    {
        var graph = CreateGraph();

        Assert.Same(graph.BusAfterMainBreaker, graph.FindBus(RuntimeBusIds.AfterMainBreaker));
    }

    [Fact]
    public void FindBus_legacy_BUS_MAIN_SEC_maps_to_AfterMainBreaker()
    {
        var graph = CreateGraph();

        Assert.Same(graph.BusAfterMainBreaker, graph.FindBus(RuntimeBusIds.LegacyAfterMainBreaker));
    }
}

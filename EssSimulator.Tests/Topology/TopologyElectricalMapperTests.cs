using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.Web.Topology;

namespace EssSimulator.Tests.Topology;

public class TopologyElectricalMapperTests
{
    private static TopologyNode Node(string id, string templateId, string label,
        Dictionary<string, object?>? overrides = null, double x = 0, double y = 0)
    {
        var tpl = TopologyTemplates.Get(templateId)!;
        var p = new Dictionary<string, object?>(tpl.DefaultParameters);
        if (overrides != null)
        {
            foreach (var kv in overrides)
                p[kv.Key] = kv.Value;
        }

        return new TopologyNode { Id = id, TemplateId = templateId, Label = label, Parameters = p, X = x, Y = y };
    }

    private static TopologyEdge Edge(string id, string from, string fromPort, string to, string toPort) => new()
    {
        Id = id,
        FromNodeId = from,
        FromPortId = fromPort,
        ToNodeId = to,
        ToPortId = toPort
    };

    /// <summary>标准径向：电网→主断→220kV 母线（电表+主变一次）→35kV。</summary>
    private static TopologyProject StandardRadial(string meterBusId)
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("brk", "ac_breaker", "主断", new Dictionary<string, object?> { ["isMainBreaker"] = true }),
                Node("hv", "ac_bus", "220kV", new Dictionary<string, object?> { ["nominalVoltage"] = 220000d }),
                Node("xf", "transformer", "主变"),
                Node("lv", "ac_bus", "35kV", new Dictionary<string, object?> { ["nominalVoltage"] = 35000d }),
                Node("m1", "ac_meter", "PCC", new Dictionary<string, object?> { ["isPccMeter"] = true }),
                Node("e1", "emu", "EMU-1", y: 600),
                Node("p1", "pcs", "PCS-1", new Dictionary<string, object?> { ["emuId"] = "e1" }, y: 720)
            },
            Edges =
            {
                Edge("e1", "g1", "a", "brk", "a"),
                Edge("e2", "brk", "a2", "hv", "a"),
                Edge("e3", "xf", "pri_a", "hv", "a2"),
                Edge("e4", "xf", "sec_a", "lv", "a"),
                Edge("e5", "m1", "pt_a", meterBusId, "a2"),
                Edge("e6", "p1", "ac_a", "lv", "a2")
            }
        };
        return p;
    }

    [Fact]
    public void Map_standard_radial_assigns_hv_after_breaker_and_lv_after_transformer()
    {
        var project = StandardRadial("hv");
        var mapping = TopologyElectricalMapper.Map(project);

        Assert.True(mapping.HasStationTransformer);
        Assert.Equal(RuntimeBusIds.AfterMainBreaker, mapping.BusRuntimeIds["hv"]);
        Assert.Equal(RuntimeBusIds.Station35, mapping.BusRuntimeIds["lv"]);
        Assert.Equal(RuntimeBusIds.AfterMainBreaker, TopologyElectricalMapper.ResolveMeterSourceBusId(project, project.Nodes.First(n => n.Id == "m1")));
    }

    [Fact]
    public void ResolveMeter_on_35kV_bus_samples_station_bus()
    {
        var project = StandardRadial("lv");
        Assert.Equal(
            RuntimeBusIds.Station35,
            TopologyElectricalMapper.ResolveMeterSourceBusId(project, project.Nodes.First(n => n.Id == "m1")));
    }

    [Fact]
    public void Map_bus_between_grid_and_breaker_is_grid_bus()
    {
        var project = new TopologyProject
        {
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("gbus", "ac_bus", "网侧母线"),
                Node("brk", "ac_breaker", "主断", new Dictionary<string, object?> { ["isMainBreaker"] = true }),
                Node("hv", "ac_bus", "站侧母线"),
                Node("m1", "ac_meter", "PCC", new Dictionary<string, object?> { ["isPccMeter"] = true })
            },
            Edges =
            {
                Edge("e1", "g1", "a", "gbus", "a"),
                Edge("e2", "gbus", "a2", "brk", "a"),
                Edge("e3", "brk", "a2", "hv", "a"),
                Edge("e4", "m1", "pt_a", "gbus", "a2")
            }
        };

        Assert.Equal(RuntimeBusIds.Grid, TopologyElectricalMapper.Map(project).BusRuntimeIds["gbus"]);
        Assert.Equal(RuntimeBusIds.Station35, TopologyElectricalMapper.Map(project).BusRuntimeIds["hv"]);
        Assert.Equal(
            RuntimeBusIds.Grid,
            TopologyElectricalMapper.ResolveMeterSourceBusId(project, project.Nodes.First(n => n.Id == "m1")));
    }

    [Fact]
    public void Map_without_station_transformer_post_breaker_bus_is_station_bus()
    {
        var project = new TopologyProject
        {
            Nodes =
            {
                Node("g1", "grid", "电网", new Dictionary<string, object?> { ["outputVoltage"] = 35000d }),
                Node("brk", "ac_breaker", "主断", new Dictionary<string, object?> { ["isMainBreaker"] = true, ["ratedVoltage"] = 35000d }),
                Node("bus", "ac_bus", "35kV"),
                Node("m1", "ac_meter", "PCC", new Dictionary<string, object?> { ["isPccMeter"] = true, ["ptPrimaryVoltage"] = 35000d })
            },
            Edges =
            {
                Edge("e1", "g1", "a", "brk", "a"),
                Edge("e2", "brk", "a2", "bus", "a"),
                Edge("e3", "m1", "pt_a", "bus", "a2")
            }
        };

        var mapping = TopologyElectricalMapper.Map(project);
        Assert.False(mapping.HasStationTransformer);
        Assert.Equal(RuntimeBusIds.Station35, mapping.BusRuntimeIds["bus"]);
        Assert.Equal(RuntimeBusIds.Station35, TopologyElectricalMapper.ResolveMeterSourceBusId(project, project.Nodes.First(n => n.Id == "m1")));
    }
}

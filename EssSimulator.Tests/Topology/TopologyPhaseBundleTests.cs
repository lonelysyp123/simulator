using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyPhaseBundleTests
{
    private static TopologyNode Node(string id, string templateId, string label, Dictionary<string, object?>? parameters = null)
    {
        var tpl = TopologyTemplates.Get(templateId)!;
        var p = new Dictionary<string, object?>(tpl.DefaultParameters);
        if (parameters != null)
        {
            foreach (var kv in parameters)
                p[kv.Key] = kv.Value;
        }

        return new TopologyNode
        {
            Id = id,
            TemplateId = templateId,
            Label = label,
            Parameters = p
        };
    }

    private static TopologyEdge Edge(string from, string fromPort, string to, string toPort) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        FromNodeId = from,
        FromPortId = fromPort,
        ToNodeId = to,
        ToPortId = toPort
    };

    [Fact]
    public void ExpandBundle_ac_phases_same_side_yields_three_edges()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("brk1", "ac_breaker", "主断")
            }
        };

        var edges = TopologyValidator.ExpandBundle(p, Edge("grid1", "a", "brk1", "a"));
        Assert.Equal(3, edges.Count);
        Assert.Contains(edges, e => e.FromPortId == "a" && e.ToPortId == "a");
        Assert.Contains(edges, e => e.FromPortId == "b" && e.ToPortId == "b");
        Assert.Contains(edges, e => e.FromPortId == "c" && e.ToPortId == "c");
    }

    [Fact]
    public void ExpandBundle_breaker_bottom_to_bus_top_pairs_by_phase()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("brk1", "ac_breaker", "主断"),
                Node("bus1", "ac_bus", "HV")
            }
        };

        var edges = TopologyValidator.ExpandBundle(p, Edge("brk1", "a2", "bus1", "a"));
        Assert.Equal(3, edges.Count);
        Assert.Contains(edges, e => e.FromPortId == "a2" && e.ToPortId == "a");
        Assert.Contains(edges, e => e.FromPortId == "b2" && e.ToPortId == "b");
        Assert.Contains(edges, e => e.FromPortId == "c2" && e.ToPortId == "c");
    }

    [Fact]
    public void ExpandBundle_dc_polarity_yields_pos_and_neg()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("emu1", "emu", "EMU"),
                Node("dc1", "dc_bus", "DC")
            }
        };

        var edges = TopologyValidator.ExpandBundle(p, Edge("emu1", "dc_pos", "dc1", "pos_t"));
        Assert.Equal(2, edges.Count);
        Assert.Contains(edges, e => e.FromPortId == "dc_pos" && e.ToPortId == "pos_t");
        Assert.Contains(edges, e => e.FromPortId == "dc_neg" && e.ToPortId == "neg_t");
    }

    [Fact]
    public void TryConnectBundle_connects_three_phases_in_one_call()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("brk1", "ac_breaker", "主断"),
                Node("bus1", "ac_bus", "HV")
            }
        };

        var r = TopologyValidator.TryConnectBundle(p, Edge("grid1", "a", "brk1", "a"), out var afterGrid);
        Assert.True(r.Ok);
        Assert.NotNull(afterGrid);
        Assert.Equal(3, afterGrid!.Edges.Count);

        r = TopologyValidator.TryConnectBundle(afterGrid, Edge("brk1", "a2", "bus1", "a"), out var afterBus);
        Assert.True(r.Ok);
        Assert.NotNull(afterBus);
        Assert.Equal(6, afterBus!.Edges.Count);

        TopologyValidator.RefreshAcBusEnergization(afterBus);
        var bus = afterBus.Nodes.First(n => n.Id == "bus1");
        Assert.True(bus.Parameters.TryGetValue("energized", out var en) && en is true);
    }

    [Fact]
    public void TryConnectBundle_rolls_back_when_any_phase_fails()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("emu1", "emu", "EMU"),
                Node("bus1", "ac_bus", "未带电母线")
            }
        };

        // 母线下侧无源拒绝
        var r = TopologyValidator.TryConnectBundle(p, Edge("emu1", "ac_a", "bus1", "a2"), out var updated);
        Assert.False(r.Ok);
        Assert.Null(updated);
        Assert.Empty(p.Edges);
    }

    [Fact]
    public void Save_validation_exposes_problem_node_ids_for_multi_main_breaker()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("brk1", "ac_breaker", "主断A", new Dictionary<string, object?> { ["isMainBreaker"] = true }),
                Node("brk2", "ac_breaker", "主断B", new Dictionary<string, object?> { ["isMainBreaker"] = true }),
                Node("m1", "ac_meter", "PCC", new Dictionary<string, object?> { ["isPccMeter"] = true })
            }
        };

        var r = TopologyValidator.ValidateProjectForSave(p);
        Assert.False(r.Ok);
        Assert.Equal("MULTI_MAIN_BREAKER", r.Code);
        Assert.Contains("brk1", r.ProblemNodeIds);
        Assert.Contains("brk2", r.ProblemNodeIds);
    }
}

using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyValidatorTests
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

    private static TopologyValidationResult Connect(TopologyProject p, TopologyEdge e)
    {
        var r = TopologyValidator.TryConnect(p, e);
        if (r.Ok) TopologyValidator.ApplyConnect(p, e);
        return r;
    }

    [Fact]
    public void Bus_top_rejects_non_voltage_source()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("bus1", "ac_bus", "35kV母线"),
                Node("emu1", "emu", "EMU-1")
            }
        };

        var r = Connect(p, Edge("emu1", "ac_a", "bus1", "a"));
        Assert.False(r.Ok);
        Assert.Equal("BUS_TOP_SOURCE_ONLY", r.Code);
        Assert.Empty(p.Edges);
    }

    [Fact]
    public void Bus_bottom_rejects_device_without_voltage_source()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("bus1", "ac_bus", "35kV母线"),
                Node("emu1", "emu", "EMU-1")
            }
        };

        var r = Connect(p, Edge("emu1", "ac_a", "bus1", "a2"));
        Assert.False(r.Ok);
        Assert.Equal("BUS_NO_SOURCE", r.Code);
        Assert.Empty(p.Edges);
    }

    [Fact]
    public void Bus_bottom_rejects_voltage_source()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线")
            }
        };

        var r = Connect(p, Edge("grid1", "a", "bus1", "a2"));
        Assert.False(r.Ok);
        Assert.Equal("BUS_BOTTOM_LOAD_ONLY", r.Code);
    }

    [Fact]
    public void Bus_top_port_is_exclusive()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("xfmr1", "transformer", "主变", new Dictionary<string, object?>
                {
                    ["primaryVoltage"] = 220000d,
                    ["secondaryVoltage"] = 35000d
                }),
                Node("bus1", "ac_bus", "母线")
            }
        };

        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);
        // 上侧 A 已被电网占用，变压器二次侧不能再占同一拐角
        var r = Connect(p, Edge("xfmr1", "sec_a", "bus1", "a"));
        Assert.False(r.Ok);
        Assert.Equal("PORT_BUSY", r.Code);
    }

    [Fact]
    public void Bus_rejects_second_voltage_source()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("grid2", "grid", "第二电网", new Dictionary<string, object?> { ["outputVoltage"] = 220000d }),
                Node("bus1", "ac_bus", "母线")
            }
        };

        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);

        // 另一上侧相拐角再接第二个电压源 → 拒绝
        var r = Connect(p, Edge("grid2", "b", "bus1", "b"));
        Assert.False(r.Ok);
        Assert.Equal("BUS_MULTI_SOURCE", r.Code);
    }

    [Fact]
    public void Transformer_primary_can_attach_to_energized_bus()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线"),
                Node("xfmr1", "transformer", "主变")
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);
        var r = Connect(p, Edge("xfmr1", "pri_a", "bus1", "a2"));
        Assert.True(r.Ok);
    }

    [Fact]
    public void Ac_bus_port_allows_multiple_downstream_devices()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "35kV电源", new Dictionary<string, object?> { ["outputVoltage"] = 35000d }),
                Node("bus1", "ac_bus", "35kV母线"),
                Node("emu1", "emu", "EMU-1"),
                Node("emu2", "emu", "EMU-2")
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);
        Assert.True(Connect(p, Edge("grid1", "b", "bus1", "b")).Ok);
        Assert.True(Connect(p, Edge("grid1", "c", "bus1", "c")).Ok);

        // 同一母线 A' 拐角挂两台 EMU
        Assert.True(Connect(p, Edge("emu1", "ac_a", "bus1", "a2")).Ok);
        Assert.True(Connect(p, Edge("emu2", "ac_a", "bus1", "a2")).Ok);
        Assert.Equal(2, p.Edges.Count(e =>
            (e.FromNodeId == "bus1" && e.FromPortId == "a2") ||
            (e.ToNodeId == "bus1" && e.ToPortId == "a2")));
    }

    [Fact]
    public void Dc_polarity_must_match()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("emu1", "emu", "EMU-1"),
                Node("dc1", "dc_bus", "DC母线"),
                Node("bms1", "bms", "BMS-1")
            }
        };

        Assert.True(Connect(p, Edge("emu1", "dc_pos", "dc1", "pos_t")).Ok);
        Assert.True(Connect(p, Edge("emu1", "dc_neg", "dc1", "neg_t")).Ok);

        var bad = Connect(p, Edge("bms1", "dc_pos", "dc1", "neg_b"));
        Assert.False(bad.Ok);
        Assert.Equal("DC_POLARITY", bad.Code);

        Assert.True(Connect(p, Edge("bms1", "dc_pos", "dc1", "pos_b")).Ok);
        Assert.True(Connect(p, Edge("bms1", "dc_neg", "dc1", "neg_b")).Ok);
    }

    [Fact]
    public void Device_port_still_exclusive()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线"),
                Node("bus2", "ac_bus", "母线2")
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);
        // 电网 A 相端口已被占用，不能再接到另一母线
        var r = Connect(p, Edge("grid1", "a", "bus2", "a"));
        Assert.False(r.Ok);
        Assert.Equal("PORT_BUSY", r.Code);
    }

    [Fact]
    public void Transformer_requires_step_down_and_bus_voltage_match()
    {
        var bad = Node("xfmr", "transformer", "坏变", new Dictionary<string, object?>
        {
            ["primaryVoltage"] = 35000d,
            ["secondaryVoltage"] = 220000d
        });
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线"),
                bad
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);

        var r = Connect(p, Edge("xfmr", "pri_a", "bus1", "a2"));
        Assert.False(r.Ok);
        Assert.Equal("XFMR_RATIO", r.Code);
    }

    [Fact]
    public void Transformer_bus_voltage_mismatch_rejected()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线"),
                Node("xfmr1", "transformer", "主变", new Dictionary<string, object?>
                {
                    ["primaryVoltage"] = 110000d,
                    ["secondaryVoltage"] = 35000d
                })
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);

        var r = Connect(p, Edge("xfmr1", "pri_a", "bus1", "a2"));
        Assert.False(r.Ok);
        Assert.Equal("XFMR_BUS_MISMATCH", r.Code);
    }

    [Fact]
    public void Meter_must_connect_to_bus_with_matching_phase_and_pt()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线"),
                Node("m1", "ac_meter", "电表")
            }
        };
        Assert.True(Connect(p, Edge("grid1", "a", "bus1", "a")).Ok);
        Assert.True(Connect(p, Edge("grid1", "b", "bus1", "b")).Ok);
        Assert.True(Connect(p, Edge("grid1", "c", "bus1", "c")).Ok);

        var phaseBad = Connect(p, Edge("m1", "pt_a", "bus1", "b2"));
        Assert.False(phaseBad.Ok);
        // 相位校验优先于电表专用规则
        Assert.Equal("PHASE_MISMATCH", phaseBad.Code);

        var ok = Connect(p, Edge("m1", "pt_a", "bus1", "a2"));
        Assert.True(ok.Ok);
    }

    [Fact]
    public void Phase_mismatch_rejected()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus1", "ac_bus", "母线")
            }
        };
        var r = Connect(p, Edge("grid1", "a", "bus1", "b"));
        Assert.False(r.Ok);
        Assert.Equal("PHASE_MISMATCH", r.Code);
    }

    [Fact]
    public void Happy_path_grid_bus_transformer_energizes_secondary_bus()
    {
        var p = new TopologyProject
        {
            Nodes =
            {
                Node("grid1", "grid", "电网"),
                Node("bus220", "ac_bus", "220kV母线"),
                Node("xfmr1", "transformer", "主变"),
                Node("bus35", "ac_bus", "35kV母线"),
                Node("emu1", "emu", "EMU-1")
            }
        };

        Assert.True(Connect(p, Edge("grid1", "a", "bus220", "a")).Ok);
        Assert.True(Connect(p, Edge("grid1", "b", "bus220", "b")).Ok);
        Assert.True(Connect(p, Edge("grid1", "c", "bus220", "c")).Ok);

        Assert.True(Connect(p, Edge("xfmr1", "pri_a", "bus220", "a2")).Ok);
        Assert.True(Connect(p, Edge("xfmr1", "pri_b", "bus220", "b2")).Ok);
        Assert.True(Connect(p, Edge("xfmr1", "pri_c", "bus220", "c2")).Ok);

        Assert.True(Connect(p, Edge("xfmr1", "sec_a", "bus35", "a")).Ok);
        Assert.True(Connect(p, Edge("xfmr1", "sec_b", "bus35", "b")).Ok);
        Assert.True(Connect(p, Edge("xfmr1", "sec_c", "bus35", "c")).Ok);

        TopologyValidator.RefreshAcBusEnergization(p);
        Assert.True(TopologyParamHelper.GetDouble(p.Nodes.First(n => n.Id == "bus35").Parameters, "nominalVoltage") > 0);

        Assert.True(Connect(p, Edge("emu1", "ac_a", "bus35", "a2")).Ok);
    }
}

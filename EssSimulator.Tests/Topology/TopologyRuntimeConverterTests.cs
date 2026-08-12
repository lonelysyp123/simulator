using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyRuntimeConverterTests
{
    [Fact]
    public void Convert_builds_ess_units_from_emu_and_bms()
    {
        var gridTpl = TopologyTemplates.Get("grid")!;
        var emuTpl = TopologyTemplates.Get("emu")!;
        var bmsTpl = TopologyTemplates.Get("bms")!;
        var dcTpl = TopologyTemplates.Get("dc_bus")!;

        var project = new TopologyProject
        {
            Id = "p1",
            Name = "测试工程",
            Nodes =
            {
                new TopologyNode
                {
                    Id = "g1", TemplateId = "grid", Label = "电网",
                    Parameters = new Dictionary<string, object?>(gridTpl.DefaultParameters)
                },
                new TopologyNode
                {
                    Id = "e1", TemplateId = "emu", Label = "Unit-A",
                    Parameters = new Dictionary<string, object?>(emuTpl.DefaultParameters)
                },
                new TopologyNode
                {
                    Id = "dc1", TemplateId = "dc_bus", Label = "DC",
                    Parameters = new Dictionary<string, object?>(dcTpl.DefaultParameters)
                },
                new TopologyNode
                {
                    Id = "b1", TemplateId = "bms", Label = "BMS-A",
                    Parameters = new Dictionary<string, object?>(bmsTpl.DefaultParameters)
                    {
                        ["clusterCount"] = 8d,
                        ["name"] = "BMS-A"
                    }
                },
                new TopologyNode
                {
                    Id = "b2", TemplateId = "bms", Label = "BMS-B",
                    Parameters = new Dictionary<string, object?>(bmsTpl.DefaultParameters)
                    {
                        ["clusterCount"] = 10d,
                        ["name"] = "BMS-B"
                    }
                }
            },
            Edges =
            {
                new TopologyEdge { Id = "1", FromNodeId = "e1", FromPortId = "dc_pos", ToNodeId = "dc1", ToPortId = "pos_t" },
                new TopologyEdge { Id = "2", FromNodeId = "e1", FromPortId = "dc_neg", ToNodeId = "dc1", ToPortId = "neg_t" },
                new TopologyEdge { Id = "3", FromNodeId = "b1", FromPortId = "dc_pos", ToNodeId = "dc1", ToPortId = "pos_b" },
                new TopologyEdge { Id = "4", FromNodeId = "b1", FromPortId = "dc_neg", ToNodeId = "dc1", ToPortId = "neg_b" },
                new TopologyEdge { Id = "5", FromNodeId = "b2", FromPortId = "dc_pos", ToNodeId = "dc1", ToPortId = "pos_b" },
                new TopologyEdge { Id = "6", FromNodeId = "b2", FromPortId = "dc_neg", ToNodeId = "dc1", ToPortId = "neg_b" }
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok);
        Assert.NotNull(overlay);
        Assert.Single(overlay!.EssUnits);
        Assert.Equal("Unit-A", overlay.EssUnits[0].Name);
        Assert.Equal(2, overlay.EssUnits[0].Bms.Count);
        Assert.Equal(8, overlay.EssUnits[0].Bms[0].ClusterCount);
        Assert.Equal(10, overlay.EssUnits[0].Bms[1].ClusterCount);
        Assert.NotNull(overlay.Pcc);
        Assert.Equal(220000, overlay.Pcc!.NominalLineVoltage);
    }

    [Fact]
    public void Convert_rejects_project_without_emu()
    {
        var project = new TopologyProject
        {
            Nodes = { new TopologyNode { Id = "g", TemplateId = "grid", Label = "电网", Parameters = new() } }
        };
        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.False(validation.Ok);
        Assert.Null(overlay);
        Assert.Equal("NO_EMU", validation.Code);
    }
}

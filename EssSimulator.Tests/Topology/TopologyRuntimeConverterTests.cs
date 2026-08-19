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
    public void Convert_rejects_project_without_emu_or_pv_unit()
    {
        var project = new TopologyProject
        {
            Nodes = { new TopologyNode { Id = "g", TemplateId = "grid", Label = "电网", Parameters = new() } }
        };
        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.False(validation.Ok);
        Assert.Null(overlay);
        Assert.Equal("NO_GENERATION_UNIT", validation.Code);
    }

    [Fact]
    public void Convert_accepts_project_with_only_pv_unit()
    {
        var pvTpl = TopologyTemplates.Get("pv_unit")!;
        var project = new TopologyProject
        {
            Nodes =
            {
                new TopologyNode
                {
                    Id = "pv1", TemplateId = "pv_unit", Label = "光伏单元-1",
                    Parameters = new Dictionary<string, object?>(pvTpl.DefaultParameters)
                }
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        Assert.Empty(overlay!.EssUnits);
        Assert.Single(overlay.PvUnits);
        Assert.Equal("光伏单元-1", overlay.PvUnits[0].Name);
        Assert.Equal(16, overlay.PvUnits[0].InverterCount);
        Assert.Equal(320, overlay.PvUnits[0].InverterRatedPowerKw);
        Assert.Equal(35000, overlay.PvUnits[0].UnitXfPrimaryV);
        Assert.Contains(overlay.Notes, n => n.Contains("光伏单元") && n.Contains("已展开"));
        Assert.Contains("光伏单元", validation.Message);
        Assert.DoesNotContain("暂未展开", validation.Message);
        Assert.NotNull(overlay.UnitTransformer);
        Assert.Equal(35000, overlay.UnitTransformer!.PrimaryVoltage);
        Assert.Equal(690, overlay.UnitTransformer.SecondaryVoltage);
        Assert.NotNull(overlay.Pcs);
        Assert.Equal(320, overlay.Pcs!.RatedPower);
    }

    [Fact]
    public void ConvertForApply_rejects_project_without_main_breaker()
    {
        var project = new TopologyProject
        {
            Nodes =
            {
                new TopologyNode { Id = "g1", TemplateId = "grid", Label = "电网", Parameters = new() },
                new TopologyNode
                {
                    Id = "e1",
                    TemplateId = "emu",
                    Label = "Unit-A",
                    Parameters = new Dictionary<string, object?>(TopologyTemplates.Get("emu")!.DefaultParameters)
                }
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.ConvertForApply(project);
        Assert.False(validation.Ok);
        Assert.Null(overlay);
        Assert.Equal("NEED_MAIN_BREAKER", validation.Code);
    }

    [Fact]
    public void Convert_keeps_ess_units_and_notes_pv_units()
    {
        var emuTpl = TopologyTemplates.Get("emu")!;
        var pvTpl = TopologyTemplates.Get("pv_unit")!;
        var project = new TopologyProject
        {
            Nodes =
            {
                new TopologyNode
                {
                    Id = "e1", TemplateId = "emu", Label = "Unit-A",
                    Parameters = new Dictionary<string, object?>(emuTpl.DefaultParameters)
                },
                new TopologyNode
                {
                    Id = "pv1", TemplateId = "pv_unit", Label = "光伏单元-1",
                    Parameters = new Dictionary<string, object?>(pvTpl.DefaultParameters)
                }
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        Assert.Single(overlay!.EssUnits);
        Assert.Single(overlay.PvUnits);
        Assert.Equal("光伏单元-1", overlay.PvUnits[0].Name);
        Assert.Contains(overlay.Notes, n => n.Contains("光伏单元") && n.Contains("已展开"));
    }

    [Fact]
    public void Convert_reads_pv_unit_inverter_count_from_node_parameters()
    {
        var pvTpl = TopologyTemplates.Get("pv_unit")!;
        var parameters = new Dictionary<string, object?>(pvTpl.DefaultParameters)
        {
            ["inverterCount"] = 20d,
            ["unitXfRatedKva"] = 6400d
        };
        var project = new TopologyProject
        {
            Nodes =
            {
                new TopologyNode
                {
                    Id = "pv1", TemplateId = "pv_unit", Label = "PV-A",
                    Parameters = parameters
                }
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.Equal(20, overlay!.PvUnits[0].InverterCount);
        Assert.Equal(6400, overlay.PvUnits[0].UnitXfRatedKva);
    }

    [Fact]
    public void ConvertForApply_accepts_pv_only_scaffold()
    {
        var project = TopologyScaffold.BuildRadial(emuCount: 0, name: "pv-apply", pvCount: 2);
        var (overlay, validation) = TopologyRuntimeConverter.ConvertForApply(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        Assert.Empty(overlay!.EssUnits);
        Assert.Equal(2, overlay.PvUnits.Count);
    }
}

using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyRuntimeConverterTests
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

    /// <summary>两台 EMU×3 台 PCS：按 emuId 分组生成单元，BMS 与 PCS 按位对齐，无 PCS 的 EMU 跳过。</summary>
    [Fact]
    public void Convert_builds_ess_units_grouped_by_emu_id()
    {
        var project = new TopologyProject
        {
            Id = "p1",
            Name = "测试工程",
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("e1", "emu", "EMU-1", y: 600),
                Node("e2", "emu", "EMU-2", y: 700),
                Node("e3", "emu", "EMU-空闲", y: 800),
                // 单元 1：3 台 PCS + DC 母线 + 3 台 BMS
                Node("p1a", "pcs", "PCS-1A", new Dictionary<string, object?> { ["emuId"] = "e1" }, x: 100, y: 720),
                Node("p1b", "pcs", "PCS-1B", new Dictionary<string, object?> { ["emuId"] = "e1" }, x: 200, y: 720),
                Node("p1c", "pcs", "PCS-1C", new Dictionary<string, object?> { ["emuId"] = "e1" }, x: 300, y: 720),
                Node("dc1", "dc_bus", "DC-1"),
                Node("b1a", "bms", "BMS-1A", new Dictionary<string, object?> { ["clusterCount"] = 8d, ["name"] = "BMS-1A" }, x: 100, y: 900),
                Node("b1b", "bms", "BMS-1B", new Dictionary<string, object?> { ["clusterCount"] = 10d, ["name"] = "BMS-1B" }, x: 200, y: 900),
                Node("b1c", "bms", "BMS-1C", new Dictionary<string, object?> { ["clusterCount"] = 12d, ["name"] = "BMS-1C" }, x: 300, y: 900),
                // 单元 2：3 台 PCS，无任何 BMS 连线（缺位用默认配置补齐）
                Node("p2a", "pcs", null!, new Dictionary<string, object?> { ["emuId"] = "e2" }, x: 500, y: 720),
                Node("p2b", "pcs", null!, new Dictionary<string, object?> { ["emuId"] = "e2" }, x: 600, y: 720),
                Node("p2c", "pcs", null!, new Dictionary<string, object?> { ["emuId"] = "e2" }, x: 700, y: 720)
            },
            Edges =
            {
                Edge("1", "p1a", "dc_pos", "dc1", "pos_t"), Edge("2", "p1a", "dc_neg", "dc1", "neg_t"),
                Edge("3", "p1b", "dc_pos", "dc1", "pos_t"), Edge("4", "p1b", "dc_neg", "dc1", "neg_t"),
                Edge("5", "p1c", "dc_pos", "dc1", "pos_t"), Edge("6", "p1c", "dc_neg", "dc1", "neg_t"),
                Edge("7", "b1a", "dc_pos", "dc1", "pos_b"), Edge("8", "b1a", "dc_neg", "dc1", "neg_b"),
                Edge("9", "b1b", "dc_pos", "dc1", "pos_b"), Edge("10", "b1b", "dc_neg", "dc1", "neg_b"),
                Edge("11", "b1c", "dc_pos", "dc1", "pos_b"), Edge("12", "b1c", "dc_neg", "dc1", "neg_b")
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        Assert.Equal(2, overlay!.EssUnits.Count);

        var u1 = overlay.EssUnits[0];
        Assert.Equal("EMU-1", u1.Name);
        Assert.Equal(3, u1.Pcs.Count);
        Assert.Equal(new[] { "PCS-1A", "PCS-1B", "PCS-1C" }, u1.Pcs.Select(p => p.Name).ToArray());
        // 共享 DC 母线时 BMS 不重复占用，按 X 顺序与 PCS 对齐
        Assert.Equal(new[] { "BMS-1A", "BMS-1B", "BMS-1C" }, u1.Bms.Select(b => b.Name).ToArray());
        Assert.Equal(new[] { 8, 10, 12 }, u1.Bms.Select(b => b.ClusterCount).ToArray());

        var u2 = overlay.EssUnits[1];
        Assert.Equal("EMU-2", u2.Name);
        Assert.Equal(3, u2.Pcs.Count);
        // 无标签 PCS 按 PCS-{单元号}{槽位} 命名；无连线 BMS 用默认配置补齐
        Assert.Equal(new[] { "PCS-2A", "PCS-2B", "PCS-2C" }, u2.Pcs.Select(p => p.Name).ToArray());
        Assert.Equal(3, u2.Bms.Count);
        Assert.Equal(new[] { "BMS-2A", "BMS-2B", "BMS-2C" }, u2.Bms.Select(b => b.Name).ToArray());

        // 无归属 PCS 的 EMU 跳过并提示
        Assert.Contains(overlay.Notes, n => n.Contains("EMU-空闲") && n.Contains("已跳过"));

        // 单元变取 EMU 参数，PCS 额定取首台 PCS
        Assert.Equal(6300, overlay.UnitTransformer!.RatedPower);
        Assert.Equal(1725, overlay.Pcs!.RatedPower);
        Assert.NotNull(overlay.Pcc);
        Assert.Equal(220000, overlay.Pcc!.NominalLineVoltage);
    }

    [Fact]
    public void Convert_records_unit_breaker_and_meter_binding()
    {
        var project = new TopologyProject
        {
            Id = "p-bind",
            Name = "绑定测试",
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("e1", "emu", "EMU-1", y: 600),
                Node("e2", "emu", "EMU-2", y: 700),
                Node("p1", "pcs", "PCS-1A", new Dictionary<string, object?> { ["emuId"] = "e1" }, x: 100, y: 720),
                Node("p2", "pcs", "PCS-2A", new Dictionary<string, object?> { ["emuId"] = "e2" }, x: 500, y: 720),
                Node("ub1", "ac_breaker", "单元断", new Dictionary<string, object?> { ["emuId"] = "e1" }),
                Node("um1", "ac_meter", "单元电表", new Dictionary<string, object?> { ["emuId"] = "e1" })
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        Assert.Equal(2, overlay!.EssUnits.Count);

        var u1 = overlay.EssUnits[0];
        Assert.True(u1.HasUnitBreaker);
        Assert.Equal("单元断", u1.UnitBreakerName);
        Assert.True(u1.HasUnitMeter);
        Assert.Equal("单元电表", u1.UnitMeterName);

        // 未绑定设备的单元：Has*=false、名称为 null
        var u2 = overlay.EssUnits[1];
        Assert.False(u2.HasUnitBreaker);
        Assert.Null(u2.UnitBreakerName);
        Assert.False(u2.HasUnitMeter);
        Assert.Null(u2.UnitMeterName);

        Assert.Contains(overlay.Notes, n => n.Contains("单元断") && n.Contains("单元电表"));
    }

    /// <summary>EMU 内含 emu_group 节点：按 groupId 归集 PCS/断路器/电表生成 unit.Groups；组级绑定不占 EMU 级槽位。</summary>
    [Fact]
    public void Convert_builds_groups_when_emu_has_emu_group_nodes()
    {
        var project = new TopologyProject
        {
            Id = "p-group",
            Name = "分组测试",
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("e1", "emu", "EMU-1", y: 600),
                Node("grpA", "emu_group", "分组A", new Dictionary<string, object?> { ["emuId"] = "e1" }, y: 700),
                Node("grpB", "emu_group", "分组B", new Dictionary<string, object?> { ["emuId"] = "e1" }, y: 750),
                Node("pA1", "pcs", "PCS-A1", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" }, x: 100, y: 720),
                Node("pA2", "pcs", "PCS-A2", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" }, x: 200, y: 720),
                Node("pB1", "pcs", "PCS-B1", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpB" }, x: 300, y: 720),
                Node("gb1", "ac_breaker", "组断A", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" }),
                Node("gm1", "ac_meter", "组表A", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" }),
                Node("gm2", "ac_meter", "组表A2", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" })
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        Assert.NotNull(overlay);
        var u1 = Assert.Single(overlay!.EssUnits);

        // 组顺序按节点 Y 排序；组内 PCS 按 groupId 归集，扁平列表不重复持有
        Assert.Equal(2, u1.Groups.Count);
        Assert.Equal(new[] { "分组A", "分组B" }, u1.Groups.Select(g => g.Name).ToArray());
        Assert.Equal(new[] { "PCS-A1", "PCS-A2" }, u1.Groups[0].Pcs.Select(p => p.Name).ToArray());
        Assert.Single(u1.Groups[1].Pcs);
        Assert.Equal("PCS-B1", u1.Groups[1].Pcs[0].Name);
        Assert.Empty(u1.Pcs);
        Assert.Equal(3, u1.PcsCount);

        // 组级断路器/电表写入组配置，不占 EMU 级槽位；同组电表允许多台
        Assert.Equal("组断A", u1.Groups[0].BreakerName);
        Assert.Equal(new[] { "组表A", "组表A2" }, u1.Groups[0].MeterNames.ToArray());
        Assert.Null(u1.Groups[1].BreakerName);
        Assert.Empty(u1.Groups[1].MeterNames);
        Assert.False(u1.HasUnitBreaker);
        Assert.False(u1.HasUnitMeter);

        // Notes 输出组结构摘要
        Assert.Contains(overlay.Notes, n => n.Contains("分组×2") && n.Contains("分组A: PCS×2") && n.Contains("分组B: PCS×1"));
    }

    /// <summary>EMU 有分组时，未选组的 PCS 归入合成「直挂」组，保证扁平展开不丢 PCS。</summary>
    [Fact]
    public void Convert_puts_ungrouped_pcs_into_direct_group_when_groups_present()
    {
        var project = new TopologyProject
        {
            Id = "p-direct",
            Name = "直挂测试",
            Nodes =
            {
                Node("g1", "grid", "电网"),
                Node("e1", "emu", "EMU-1", y: 600),
                Node("grpA", "emu_group", "分组A", new Dictionary<string, object?> { ["emuId"] = "e1" }, y: 700),
                Node("pA1", "pcs", "PCS-A1", new Dictionary<string, object?> { ["emuId"] = "e1", ["groupId"] = "grpA" }, x: 100, y: 720),
                Node("pD1", "pcs", "PCS-D1", new Dictionary<string, object?> { ["emuId"] = "e1" }, x: 300, y: 720)
            }
        };

        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.True(validation.Ok, validation.Message);
        var u1 = Assert.Single(overlay!.EssUnits);

        Assert.Equal(2, u1.Groups.Count);
        Assert.Equal("直挂", u1.Groups[1].Name);
        Assert.Equal("PCS-D1", u1.Groups[1].Pcs[0].Name);
        Assert.Equal(2, u1.PcsCount);
    }

    [Fact]
    public void Convert_rejects_project_without_pcs_assigned_emu_or_pv_unit()
    {
        var project = new TopologyProject
        {
            Nodes =
            {
                Node("g", "grid", "电网"),
                Node("e1", "emu", "EMU-无PCS"),
                Node("p1", "pcs", "PCS-未归属")
            }
        };
        var (overlay, validation) = TopologyRuntimeConverter.Convert(project);
        Assert.False(validation.Ok);
        Assert.Null(overlay);
        Assert.Equal("NO_GENERATION_UNIT", validation.Code);
    }

    [Fact]
    public void Convert_accepts_project_with_only_pv_unit()
    {
        var project = new TopologyProject
        {
            Nodes = { Node("pv1", "pv_unit", "光伏单元-1") }
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
                Node("g1", "grid", "电网"),
                Node("e1", "emu", "Unit-A"),
                Node("p1", "pcs", "PCS-1A", new Dictionary<string, object?> { ["emuId"] = "e1" })
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
        var project = new TopologyProject
        {
            Nodes =
            {
                Node("e1", "emu", "Unit-A"),
                Node("p1", "pcs", "PCS-1A", new Dictionary<string, object?> { ["emuId"] = "e1" }),
                Node("pv1", "pv_unit", "光伏单元-1")
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
        var parameters = new Dictionary<string, object?>(TopologyTemplates.Get("pv_unit")!.DefaultParameters)
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

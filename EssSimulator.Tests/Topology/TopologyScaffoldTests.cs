using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyScaffoldTests
{
    [Fact]
    public void BuildRadial_creates_valid_saveable_project_with_n_emus()
    {
        var p = TopologyScaffold.BuildRadial(emuCount: 2, name: "测试径向", includeLoad: true);
        Assert.Equal("测试径向", p.Name);
        Assert.Equal(2, p.Nodes.Count(n => n.TemplateId == "emu"));
        Assert.Equal(4, p.Nodes.Count(n => n.TemplateId == "bms"));
        Assert.Contains(p.Nodes, n => n.TemplateId == "load");
        Assert.Contains(p.Nodes, n => n.TemplateId == "ac_breaker" &&
                                      TopologyParamHelper.GetBool(n.Parameters, "isMainBreaker"));
        Assert.Contains(p.Nodes, n => n.TemplateId == "ac_meter" &&
                                      TopologyParamHelper.GetBool(n.Parameters, "isPccMeter"));

        var validation = TopologyValidator.ValidateProjectForSave(p);
        Assert.True(validation.Ok, validation.Message + " / " + string.Join("; ", validation.Details));
        Assert.True(p.Edges.Count >= 18); // 站侧三相×若干 + EMU AC/DC
    }

    [Fact]
    public void BuildRadial_rejects_invalid_emu_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TopologyScaffold.BuildRadial(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => TopologyScaffold.BuildRadial(21));
    }

    [Fact]
    public void BuildRadial_without_load_omits_load_node()
    {
        var p = TopologyScaffold.BuildRadial(1, includeLoad: false);
        Assert.DoesNotContain(p.Nodes, n => n.TemplateId == "load");
        Assert.True(TopologyValidator.ValidateProjectForSave(p).Ok);
    }
}

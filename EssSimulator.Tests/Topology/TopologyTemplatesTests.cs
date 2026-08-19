using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyTemplatesTests
{
    [Fact]
    public void All_includes_pv_unit_named_光伏单元()
    {
        var t = TopologyTemplates.Get("pv_unit");
        Assert.NotNull(t);
        Assert.Equal("光伏单元", t!.Name);
        Assert.Equal("光伏", t.Category);
        Assert.False(t.IsVoltageSource);

        Assert.Equal(3, t.Ports.Count);
        Assert.Collection(t.Ports,
            p => { Assert.Equal("ac_a", p.Id); Assert.Equal("A", p.Phase); Assert.Equal("top", p.Side); },
            p => { Assert.Equal("ac_b", p.Id); Assert.Equal("B", p.Phase); Assert.Equal("top", p.Side); },
            p => { Assert.Equal("ac_c", p.Id); Assert.Equal("C", p.Phase); Assert.Equal("top", p.Side); });

        Assert.Equal(30d, TopologyParamHelper.GetDouble(t.DefaultParameters, "modulesPerString"));
        Assert.Equal(16d, TopologyParamHelper.GetDouble(t.DefaultParameters, "stringCount"));
        Assert.Equal(16d, TopologyParamHelper.GetDouble(t.DefaultParameters, "inverterCount"));
        Assert.Equal(320d, TopologyParamHelper.GetDouble(t.DefaultParameters, "inverterRatedPowerKw"));
        Assert.Equal(690d, TopologyParamHelper.GetDouble(t.DefaultParameters, "inverterAcVoltage"));
        Assert.Equal(690d, TopologyParamHelper.GetDouble(t.DefaultParameters, "unitXfSecondaryV"));
        Assert.Equal(35000d, TopologyParamHelper.GetDouble(t.DefaultParameters, "acVoltage"));
        Assert.Equal(5120d, TopologyParamHelper.GetDouble(t.DefaultParameters, "unitXfRatedKva"));
        Assert.Equal("TSM-NEG21C.20Q", TopologyParamHelper.GetString(t.DefaultParameters, "moduleModel"));
    }
}

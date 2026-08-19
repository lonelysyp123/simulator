using EssSimulator.Web;
using Xunit;

namespace EssSimulator.Tests.Web;

public class MainLineEnricherTests
{
    [Fact]
    public void ResolveEssChannelCount_zero_when_pv_only()
    {
        Assert.Equal(0, MainLineEnricher.ResolveEssChannelCount(pcsCount: 0, pvCount: 2));
        Assert.Equal(0, MainLineEnricher.ResolveEssUnitCount(0));
    }

    [Fact]
    public void ResolveEssChannelCount_keeps_pcs_when_mixed_plant()
    {
        Assert.Equal(4, MainLineEnricher.ResolveEssChannelCount(pcsCount: 4, pvCount: 2));
        Assert.Equal(2, MainLineEnricher.ResolveEssUnitCount(4));
    }

    [Fact]
    public void ResolveEssChannelCount_falls_back_to_one_channel_when_plant_empty()
    {
        Assert.Equal(1, MainLineEnricher.ResolveEssChannelCount(pcsCount: 0, pvCount: 0));
        Assert.Equal(1, MainLineEnricher.ResolveEssUnitCount(1));
    }
}

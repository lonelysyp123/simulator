using EssSimulator.Configuration;
using Xunit;

namespace EssSimulator.Tests.Edition;

public class EditionConfigTests
{
    [Fact]
    public void ApplyPresets_community_disables_droop_and_locks_units()
    {
        var e = new EditionConfig
        {
            Name = "Community",
            AllowDroopSlices = true,
            LockTopology = false,
            MaxEssUnits = 0
        };
        e.ApplyPresets();
        Assert.False(e.AllowDroopSlices);
        Assert.True(e.LockTopology);
        Assert.Equal(2, e.MaxEssUnits);
        Assert.True(e.IsCommunity);
    }

    [Fact]
    public void ApplyPresets_commercial_keeps_advanced_api()
    {
        var e = new EditionConfig
        {
            Name = "Commercial",
            AllowDroopSlices = true,
            LockTopology = false,
            MaxEssUnits = 0
        };
        e.ApplyPresets();
        Assert.True(e.AllowDroopSlices);
        Assert.False(e.LockTopology);
        Assert.Equal(0, e.MaxEssUnits);
        Assert.True(e.IsCommercial);
    }

    [Theory]
    [InlineData("社区版")]
    [InlineData("Community")]
    public void IsCommunity_accepts_aliases(string name)
    {
        Assert.True(new EditionConfig { Name = name }.IsCommunity);
    }
}

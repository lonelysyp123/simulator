using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain;
using EssSimulator.EmsStrategy.Domain.Algorithms;

namespace EssSimulator.Tests.EmsStrategy;

public class PowerDistributorTests
{
    private static PcsBranchState Branch(int i, double soc, double rated, bool running = true) => new()
    {
        Index = i,
        UnitIndex0 = 0,
        PcsIndexInUnit = i,
        CommOk = true,
        Running = running,
        Soc = soc,
        RatedKw = rated,
        MaxChargeKw = rated,
        MaxDischargeKw = rated
    };

    [Fact]
    public void EqualSplit_TwoBranches()
    {
        var cfg = new DistributionConfig();
        var cmds = PowerDistributor.Distribute(100, new[] { Branch(0, 0.5, 1000), Branch(1, 0.5, 1000) }, cfg);
        Assert.Equal(50, cmds[0].ActivePowerKw, 6);
        Assert.Equal(50, cmds[1].ActivePowerKw, 6);
    }

    [Fact]
    public void SocWeighted_DischargePrefersHighSoc()
    {
        var cfg = new DistributionConfig { SocBalance = true, SocMin = 0.1, SocMax = 0.9 };
        var cmds = PowerDistributor.Distribute(
            100,
            new[] { Branch(0, 0.8, 1000), Branch(1, 0.4, 1000) },
            cfg);
        Assert.True(cmds[0].ActivePowerKw > cmds[1].ActivePowerKw);
        Assert.Equal(100, cmds[0].ActivePowerKw + cmds[1].ActivePowerKw, 6);
    }

    [Fact]
    public void AllUnavailable_OutputsZero()
    {
        var cfg = new DistributionConfig();
        var cmds = PowerDistributor.Distribute(
            100,
            new[] { Branch(0, 0.5, 1000, running: false), Branch(1, 0.5, 1000, running: false) },
            cfg);
        Assert.Equal(0, cmds[0].ActivePowerKw);
        Assert.Equal(0, cmds[1].ActivePowerKw);
    }

    [Fact]
    public void SecondaryAllocation_OverflowGoesToOther()
    {
        var cfg = new DistributionConfig();
        var cmds = PowerDistributor.Distribute(
            150,
            new[] { Branch(0, 0.5, 40), Branch(1, 0.5, 200) },
            cfg);
        Assert.Equal(40, cmds[0].ActivePowerKw, 6);
        Assert.Equal(110, cmds[1].ActivePowerKw, 6);
    }

    [Fact]
    public void Reactive_EqualSplitAmongRunning()
    {
        var cfg = new DistributionConfig();
        var cmds = PowerDistributor.Distribute(
            0,
            80,
            new[] { Branch(0, 0.5, 1000), Branch(1, 0.5, 1000) },
            cfg);
        Assert.Equal(40, cmds[0].ReactivePowerKvar, 6);
        Assert.Equal(40, cmds[1].ReactivePowerKvar, 6);
    }
}

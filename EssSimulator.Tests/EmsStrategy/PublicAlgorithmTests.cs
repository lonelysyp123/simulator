using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain;
using EssSimulator.EmsStrategy.Domain.Algorithms;

namespace EssSimulator.Tests.EmsStrategy;

public class EmsStrategyConfigTests
{
    [Fact]
    public void CreateDefault_HasPromptPrimaryValues()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        Assert.False(cfg.Enabled);
        Assert.False(cfg.Slope.Enabled);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), cfg.PrimaryFrequency.ControlCycle);
        Assert.Equal(TimeSpan.FromMilliseconds(2000), cfg.PrimaryFrequency.ResetTime);
        Assert.Equal(0.2, cfg.PrimaryFrequency.Deadband1Percent);
        Assert.Equal(3, cfg.PrimaryFrequency.DroopPercent);
        Assert.Equal(TimeSpan.FromMilliseconds(3000), cfg.ActivePid.Period);
        Assert.Equal(PidDiscretization.CompatiblePeriod, cfg.ActivePid.Discretization);
    }
}

public class SlopeLimiterTests
{
    [Fact]
    public void Disabled_JumpsToTarget()
    {
        var s = new SlopeLimiter();
        s.Configure(false, 10, 10);
        Assert.Equal(80, s.Step(80, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Rise_LimitedByRate()
    {
        var s = new SlopeLimiter();
        s.Configure(true, riseKwPerSec: 10, fallKwPerSec: 20);
        Assert.Equal(10, s.Step(100, TimeSpan.FromSeconds(1)));
        Assert.Equal(20, s.Step(100, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Fall_LimitedByRate()
    {
        var s = new SlopeLimiter();
        s.Configure(true, 10, 20);
        s.Reset(100);
        Assert.Equal(80, s.Step(0, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void ZeroCross_DoesNotJumpSignInOneSlowStep()
    {
        var s = new SlopeLimiter();
        s.Configure(true, 20, 20);
        s.Reset(50);
        double next = s.Step(-50, TimeSpan.FromSeconds(1));
        Assert.Equal(30, next);
        Assert.True(next > 0);
    }
}

public class PidControllerTests
{
    [Fact]
    public void Deadband_HoldsOutput()
    {
        var pid = new PidController();
        pid.Configure(new PidConfig
        {
            Kp = 1,
            Ki = 1,
            Period = TimeSpan.FromSeconds(1),
            DeadbandKw = 2,
            OutMinKw = -100,
            OutMaxKw = 100,
            Discretization = PidDiscretization.Dt
        });
        Assert.Equal(0, pid.Step(0.5, TimeSpan.FromSeconds(1)));
        Assert.Equal(0, pid.Integral);
    }

    [Fact]
    public void Output_Clamped()
    {
        var pid = new PidController();
        pid.Configure(new PidConfig
        {
            Kp = 10,
            Ki = 0,
            Period = TimeSpan.FromSeconds(1),
            OutMinKw = -5,
            OutMaxKw = 5,
            Discretization = PidDiscretization.Dt
        });
        Assert.Equal(5, pid.Step(10, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void CompatiblePeriod_WaitsUnlessForced()
    {
        var pid = new PidController();
        pid.Configure(new PidConfig
        {
            Kp = 1,
            Ki = 0,
            Period = TimeSpan.FromSeconds(3),
            OutMinKw = -100,
            OutMaxKw = 100,
            Discretization = PidDiscretization.CompatiblePeriod
        });
        Assert.Equal(0, pid.Step(10, TimeSpan.FromSeconds(1)));
        Assert.Equal(10, pid.Step(10, TimeSpan.FromSeconds(1), force: true));
    }
}

public class ApparentPowerLimiterTests
{
    [Fact]
    public void LimitsActiveByOtherAxis()
    {
        double p = ApparentPowerLimiter.LimitActive(100, 60, 100);
        Assert.Equal(80, p, 5);
    }

    [Fact]
    public void OtherAxisAtRated_ForcesZero()
    {
        Assert.Equal(0, ApparentPowerLimiter.LimitActive(50, 100, 100));
        Assert.Equal(0, ApparentPowerLimiter.LimitReactive(50, 100, 100));
    }
}

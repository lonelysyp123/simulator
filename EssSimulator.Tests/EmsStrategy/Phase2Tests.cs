using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain;
using EssSimulator.EmsStrategy.Domain.Algorithms;
using EssSimulator.EmsStrategy.Domain.Services;

namespace EssSimulator.Tests.EmsStrategy;

public class PfToReactiveConverterTests
{
    [Fact]
    public void UnityPf_ZeroQ()
    {
        Assert.Equal(0, PfToReactiveConverter.FromPf(1000, 1), 6);
    }

    [Fact]
    public void Pf090_MatchesFormula()
    {
        double q = PfToReactiveConverter.FromPf(1000, 0.9);
        double expected = 1000 * Math.Sqrt(1 - 0.81) / 0.9;
        Assert.Equal(expected, q, 6);
    }
}

public class CurveSchedulerTests
{
    [Fact]
    public void WeekdayHit_ReturnsPower()
    {
        var monday = new DateTime(2026, 9, 7, 10, 0, 0); // Monday
        var cfg = new CurveConfig
        {
            Mode = CurveMatchMode.Weekday,
            Points =
            {
                new CurvePointConfig { Weekday = (int)DayOfWeek.Monday, Start = "08:00:00", End = "12:00:00", Power = 400 }
            }
        };
        Assert.True(CurveScheduler.TryGetPower(monday, cfg, out double p));
        Assert.Equal(400, p);
    }

    [Fact]
    public void Miss_ReturnsWait()
    {
        var t = new DateTime(2026, 9, 7, 18, 0, 0);
        var cfg = new CurveConfig
        {
            Mode = CurveMatchMode.Weekday,
            Points =
            {
                new CurvePointConfig { Weekday = (int)DayOfWeek.Monday, Start = "08:00:00", End = "12:00:00", Power = 400 }
            }
        };
        Assert.False(CurveScheduler.TryGetPower(t, cfg, out _));
    }
}

public class InertiaCalculatorTests
{
    [Fact]
    public void FallingFrequency_ProducesPositiveP()
    {
        var calc = new InertiaCalculator();
        var cfg = new InertiaConfig
        {
            Enabled = true,
            RatedFrequencyHz = 50,
            InertiaTimeSec = 6,
            AmplitudeDeadbandHz = 0.05,
            RateDeadbandHzPerSec = 0.05,
            FreqMinHz = 47,
            FreqMaxHz = 53,
            ControlCycle = TimeSpan.FromSeconds(1)
        };
        calc.Evaluate(50.0, TimeSpan.FromSeconds(1), 5000, cfg);
        var s = calc.Evaluate(49.7, TimeSpan.FromSeconds(1), 5000, cfg);
        Assert.True(s.ConditionMet);
        Assert.True(s.DfDtHzPerSec < 0);
        Assert.True(s.PowerKw > 0);
    }

    [Fact]
    public void InsideAmplitudeDeadband_NotActing()
    {
        var calc = new InertiaCalculator();
        var cfg = new InertiaConfig
        {
            AmplitudeDeadbandHz = 0.5,
            RateDeadbandHzPerSec = 0.01,
            ControlCycle = TimeSpan.FromSeconds(1),
            InertiaTimeSec = 6
        };
        calc.Evaluate(50.0, TimeSpan.FromSeconds(1), 5000, cfg);
        var s = calc.Evaluate(49.9, TimeSpan.FromSeconds(1), 5000, cfg);
        Assert.False(s.ConditionMet);
        Assert.Equal(0, s.PowerKw);
    }
}

public class VoltageDroopTests
{
    [Fact]
    public void UnderVoltage_PositiveDeltaQ()
    {
        var cfg = new VoltageDroopConfig
        {
            RatedVoltageV = 35000,
            Deadband1Percent = 0.5,
            K1Percent = 4,
            SegmentCount = 3,
            UnderVoltEnable = true,
            OverVoltEnable = true,
            MaxOutputKvar = 5000,
            MaxAbsorbKvar = 5000,
            LimitCoefficient = 1
        };
        double dq = VoltageDroopCalculator.ComputeDeltaKvar(34000, 5000, cfg);
        Assert.True(dq > 0);
    }

    [Fact]
    public void InsideDeadband_Zero()
    {
        var cfg = new VoltageDroopConfig { RatedVoltageV = 35000, Deadband1Percent = 1 };
        Assert.False(VoltageDroopCalculator.IsOutsideDeadband(35000, cfg));
        Assert.Equal(0, VoltageDroopCalculator.ComputeDeltaKvar(35000, 5000, cfg));
    }
}

public class ReactivePowerStrategyTests
{
    [Fact]
    public void OpenLoop_IgnoresVoltageDroop()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.ReactiveMode = ReactiveMode.OpenLoopFixed;
        cfg.LocalReactiveSetKvar = 300;
        cfg.VoltageDroop.Enabled = true;
        cfg.ReactiveSlope.Enabled = false;
        var s = new ReactivePowerStrategy();
        double y = s.Step(cfg, new PlantMeasurements { PccLineVoltageV = 30000, PccActivePowerKw = 0 }, TimeSpan.FromMilliseconds(200));
        Assert.Equal(300, y, 5);
        Assert.Equal(ActionState.Reset, s.DroopAction);
    }

    [Fact]
    public void PowerFactor_SetsQFromP()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.ReactiveMode = ReactiveMode.PowerFactor;
        cfg.PowerFactorSet = 0.9;
        cfg.ReactiveSlope.Enabled = false;
        cfg.ReactivePid.DeadbandKw = 0;
        cfg.ReactivePid.Kp = 1;
        cfg.ReactivePid.Ki = 0;
        cfg.ReactivePid.Discretization = PidDiscretization.Dt;
        var s = new ReactivePowerStrategy();
        var meas = new PlantMeasurements { PccActivePowerKw = 1000, PccReactivePowerKvar = 0 };
        s.Step(cfg, meas, TimeSpan.FromMilliseconds(200));
        Assert.True(s.LastQBase > 0);
        Assert.Equal(PfToReactiveConverter.FromPf(1000, 0.9), s.LastQBase, 5);
    }
}

public class ActivePowerPhase2Tests
{
    [Fact]
    public void CurveMiss_OutputsZeroAndWaits()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.ActiveMode = ActiveMode.CloseLoopCurve;
        cfg.ActiveCurve.Points.Add(new CurvePointConfig
        {
            Weekday = (int)DayOfWeek.Sunday,
            Start = "00:00:00",
            End = "01:00:00",
            Power = 800
        });
        var s = new ActivePowerStrategy();
        double y = s.Step(cfg, new PlantMeasurements { SimTime = new DateTime(2026, 9, 7, 10, 0, 0), FrequencyHz = 50 }, TimeSpan.FromMilliseconds(200));
        Assert.Equal(0, y);
        Assert.True(s.CurveWait);
    }

    [Fact]
    public void InertiaLock_SkipsPrimaryFrequency()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.Enabled = true;
        cfg.Slope.Enabled = false;
        cfg.ActivePid.DeadbandKw = 0;
        cfg.ActivePid.Kp = 1;
        cfg.ActivePid.Ki = 0;
        cfg.ActivePid.Discretization = PidDiscretization.Dt;
        cfg.Inertia.Enabled = true;
        cfg.Inertia.LockPrimaryFrequency = true;
        cfg.Inertia.AmplitudeDeadbandHz = 0.05;
        cfg.Inertia.RateDeadbandHzPerSec = 0.05;
        cfg.Inertia.ControlCycle = TimeSpan.FromSeconds(1);
        cfg.Inertia.InertiaTimeSec = 6;
        var s = new ActivePowerStrategy();
        var b = new[]
        {
            new PcsBranchState { Index = 0, Running = true, CommOk = true, Soc = 0.5, RatedKw = 2500, MaxChargeKw = 2500, MaxDischargeKw = 2500 }
        };
        s.Step(cfg, new PlantMeasurements { FrequencyHz = 50, Branches = b }, TimeSpan.FromSeconds(1));
        s.Step(cfg, new PlantMeasurements { FrequencyHz = 49.6, Branches = b }, TimeSpan.FromSeconds(1));
        Assert.Equal(ActionState.Action, s.InertiaAction);
        Assert.Equal(ActionState.Reset, s.FrequencyAction);
        Assert.True(s.InertiaDeltaKw > 0);
        Assert.Equal(0, s.FrequencyDeltaKw);
    }
}

public class EmsStrategyEnginePhase2Tests
{
    [Fact]
    public void CloseLoop_TargetIncludesInertiaWhenEnabled()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.Enabled = true;
        cfg.Slope.Enabled = false;
        cfg.PrimaryFrequency.Enabled = false;
        cfg.Inertia.Enabled = true;
        cfg.Inertia.AmplitudeDeadbandHz = 0.05;
        cfg.Inertia.RateDeadbandHzPerSec = 0.05;
        cfg.Inertia.ControlCycle = TimeSpan.FromSeconds(1);
        cfg.ActivePid.DeadbandKw = 0;
        cfg.ActivePid.Kp = 1;
        cfg.ActivePid.Ki = 0;
        cfg.ActivePid.Discretization = PidDiscretization.Dt;
        var engine = new EmsStrategyEngine();
        engine.Initialize(cfg);
        var branches = new[]
        {
            new PcsBranchState { Index = 0, Running = true, CommOk = true, Soc = 0.5, RatedKw = 2500, MaxChargeKw = 2500, MaxDischargeKw = 2500 }
        };
        engine.UpdateMeasurements(new PlantMeasurements { FrequencyHz = 50, Branches = branches });
        engine.Step(TimeSpan.FromSeconds(1));
        engine.UpdateMeasurements(new PlantMeasurements { FrequencyHz = 49.6, Branches = branches });
        var output = engine.Step(TimeSpan.FromSeconds(1));
        Assert.Equal(ActionState.Action, output.InertiaAction);
        Assert.Equal(output.PBaseKw + output.InertiaDeltaKw, output.PlantActiveTargetKw, 5);
    }

    [Fact]
    public void OpenLoopReactive_DistributesQ()
    {
        var cfg = EmsStrategyConfig.CreateDefault();
        cfg.Enabled = true;
        cfg.ActiveEnable = false;
        cfg.ReactiveMode = ReactiveMode.OpenLoopFixed;
        cfg.LocalReactiveSetKvar = 200;
        cfg.ReactiveSlope.Enabled = false;
        var engine = new EmsStrategyEngine();
        engine.Initialize(cfg);
        engine.UpdateMeasurements(new PlantMeasurements
        {
            Branches = new[]
            {
                new PcsBranchState { Index = 0, Running = true, CommOk = true, Soc = 0.5, RatedKw = 2500, MaxChargeKw = 2500, MaxDischargeKw = 2500 },
                new PcsBranchState { Index = 1, Running = true, CommOk = true, Soc = 0.5, RatedKw = 2500, MaxChargeKw = 2500, MaxDischargeKw = 2500 }
            }
        });
        var output = engine.Step(TimeSpan.FromMilliseconds(200));
        Assert.Equal(200, output.PlantReactiveCommandKvar, 5);
        Assert.Equal(200, output.Branches.Sum(b => b.ReactivePowerKvar), 5);
    }
}

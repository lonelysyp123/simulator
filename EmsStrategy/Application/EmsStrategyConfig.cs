using EssSimulator.EmsStrategy.Domain;

namespace EssSimulator.EmsStrategy.Application;

/// <summary>
/// 电站 EMS/PPC 策略配置。功率符号：放电为正、充电为负。
/// </summary>
public sealed class EmsStrategyConfig
{
    public bool Enabled { get; set; }
    public bool SystemSwitch { get; set; } = true;
    public bool ActiveEnable { get; set; } = true;
    public bool IsMaster { get; set; } = true;
    public bool BypassMasterCheck { get; set; } = true;
    public bool ReactiveEnable { get; set; } = true;
    public ActiveMode ActiveMode { get; set; } = ActiveMode.CloseLoopFixed;
    public ReactiveMode ReactiveMode { get; set; } = ReactiveMode.CloseLoopFixed;
    public LocalRemote LocalRemote { get; set; } = LocalRemote.Local;
    public double LocalActiveSetKw { get; set; }
    public double RemoteActiveSetKw { get; set; }
    public double LocalReactiveSetKvar { get; set; }
    public double RemoteReactiveSetKvar { get; set; }
    public double PowerFactorSet { get; set; } = 1;
    public double PfSign { get; set; } = 1;
    public double VoltageSetV { get; set; } = 35000;
    public double VoltageKp { get; set; } = 0.5;
    public double ApparentRatedKva { get; set; } = 5000;
    public double PlantRatedKw { get; set; } = 5000;

    public SlopeConfig Slope { get; set; } = new();
    public SlopeConfig ReactiveSlope { get; set; } = new();
    public PidConfig ActivePid { get; set; } = new();
    public PidConfig ReactivePid { get; set; } = new();
    public PrimaryFrequencyConfig PrimaryFrequency { get; set; } = new();
    public InertiaConfig Inertia { get; set; } = new();
    public VoltageDroopConfig VoltageDroop { get; set; } = new();
    public CurveConfig ActiveCurve { get; set; } = new();
    public CurveConfig ReactiveCurve { get; set; } = new();
    public DistributionConfig Distribution { get; set; } = new();

    public static EmsStrategyConfig CreateDefault() => new()
    {
        Enabled = false,
        SystemSwitch = true,
        ActiveEnable = true,
        ReactiveEnable = true,
        IsMaster = true,
        BypassMasterCheck = true,
        ActiveMode = ActiveMode.CloseLoopFixed,
        ReactiveMode = ReactiveMode.CloseLoopFixed,
        LocalRemote = LocalRemote.Local,
        LocalActiveSetKw = 0,
        RemoteActiveSetKw = 0,
        LocalReactiveSetKvar = 0,
        RemoteReactiveSetKvar = 0,
        PowerFactorSet = 1,
        PfSign = 1,
        VoltageSetV = 35000,
        VoltageKp = 0.5,
        ApparentRatedKva = 5000,
        PlantRatedKw = 5000,
        Slope = new SlopeConfig
        {
            Enabled = false,
            RiseKwPerSec = 100,
            FallKwPerSec = 100
        },
        ActivePid = new PidConfig
        {
            Kp = 0.4,
            Ki = 0.05,
            Kb = 0.5,
            Period = TimeSpan.FromMilliseconds(3000),
            DeadbandKw = 5,
            OutMinKw = -5000,
            OutMaxKw = 5000,
            Discretization = PidDiscretization.CompatiblePeriod
        },
        PrimaryFrequency = new PrimaryFrequencyConfig
        {
            Enabled = true,
            RatedFrequencyHz = 50,
            Deadband1Percent = 0.2,
            Deadband2Percent = 0.5,
            DroopPercent = 3,
            Droop2Percent = 5,
            SegmentCount = 3,
            OverFreqEnable = true,
            UnderFreqEnable = true,
            ControlCycle = TimeSpan.FromMilliseconds(1000),
            ResetTime = TimeSpan.FromMilliseconds(2000),
            MaxOutputKw = 5000,
            MaxAbsorbKw = 5000,
            LimitCoefficient = 1
        },
        Inertia = new InertiaConfig(),
        VoltageDroop = new VoltageDroopConfig(),
        ActiveCurve = new CurveConfig(),
        ReactiveCurve = new CurveConfig(),
        ReactiveSlope = new SlopeConfig { Enabled = false, RiseKwPerSec = 100, FallKwPerSec = 100 },
        ReactivePid = new PidConfig
        {
            Kp = 0.4,
            Ki = 0.05,
            Kb = 0.5,
            Period = TimeSpan.FromMilliseconds(3000),
            DeadbandKw = 5,
            OutMinKw = -5000,
            OutMaxKw = 5000,
            Discretization = PidDiscretization.CompatiblePeriod
        },
        Distribution = new DistributionConfig
        {
            SocBalance = false,
            SocMin = 0.1,
            SocMax = 0.9
        }
    };

    public EmsStrategyConfig Clone() => new()
    {
        Enabled = Enabled,
        SystemSwitch = SystemSwitch,
        ActiveEnable = ActiveEnable,
        ReactiveEnable = ReactiveEnable,
        IsMaster = IsMaster,
        BypassMasterCheck = BypassMasterCheck,
        ActiveMode = ActiveMode,
        ReactiveMode = ReactiveMode,
        LocalRemote = LocalRemote,
        LocalActiveSetKw = LocalActiveSetKw,
        RemoteActiveSetKw = RemoteActiveSetKw,
        LocalReactiveSetKvar = LocalReactiveSetKvar,
        RemoteReactiveSetKvar = RemoteReactiveSetKvar,
        PowerFactorSet = PowerFactorSet,
        PfSign = PfSign,
        VoltageSetV = VoltageSetV,
        VoltageKp = VoltageKp,
        ApparentRatedKva = ApparentRatedKva,
        PlantRatedKw = PlantRatedKw,
        Slope = new SlopeConfig
        {
            Enabled = Slope.Enabled,
            RiseKwPerSec = Slope.RiseKwPerSec,
            FallKwPerSec = Slope.FallKwPerSec
        },
        ActivePid = new PidConfig
        {
            Kp = ActivePid.Kp,
            Ki = ActivePid.Ki,
            Kb = ActivePid.Kb,
            Period = ActivePid.Period,
            DeadbandKw = ActivePid.DeadbandKw,
            OutMinKw = ActivePid.OutMinKw,
            OutMaxKw = ActivePid.OutMaxKw,
            Discretization = ActivePid.Discretization
        },
        PrimaryFrequency = new PrimaryFrequencyConfig
        {
            Enabled = PrimaryFrequency.Enabled,
            RatedFrequencyHz = PrimaryFrequency.RatedFrequencyHz,
            Deadband1Percent = PrimaryFrequency.Deadband1Percent,
            Deadband2Percent = PrimaryFrequency.Deadband2Percent,
            DroopPercent = PrimaryFrequency.DroopPercent,
            Droop2Percent = PrimaryFrequency.Droop2Percent,
            SegmentCount = PrimaryFrequency.SegmentCount,
            OverFreqEnable = PrimaryFrequency.OverFreqEnable,
            UnderFreqEnable = PrimaryFrequency.UnderFreqEnable,
            ControlCycle = PrimaryFrequency.ControlCycle,
            ResetTime = PrimaryFrequency.ResetTime,
            MaxOutputKw = PrimaryFrequency.MaxOutputKw,
            MaxAbsorbKw = PrimaryFrequency.MaxAbsorbKw,
            LimitCoefficient = PrimaryFrequency.LimitCoefficient
        },
        Inertia = (Inertia ?? new InertiaConfig()).Clone(),
        VoltageDroop = (VoltageDroop ?? new VoltageDroopConfig()).Clone(),
        ActiveCurve = (ActiveCurve ?? new CurveConfig()).Clone(),
        ReactiveCurve = (ReactiveCurve ?? new CurveConfig()).Clone(),
        ReactiveSlope = new SlopeConfig
        {
            Enabled = (ReactiveSlope ?? Slope).Enabled,
            RiseKwPerSec = (ReactiveSlope ?? Slope).RiseKwPerSec,
            FallKwPerSec = (ReactiveSlope ?? Slope).FallKwPerSec
        },
        ReactivePid = new PidConfig
        {
            Kp = (ReactivePid ?? ActivePid).Kp,
            Ki = (ReactivePid ?? ActivePid).Ki,
            Kb = (ReactivePid ?? ActivePid).Kb,
            Period = (ReactivePid ?? ActivePid).Period,
            DeadbandKw = (ReactivePid ?? ActivePid).DeadbandKw,
            OutMinKw = (ReactivePid ?? ActivePid).OutMinKw,
            OutMaxKw = (ReactivePid ?? ActivePid).OutMaxKw,
            Discretization = (ReactivePid ?? ActivePid).Discretization
        },
        Distribution = new DistributionConfig
        {
            SocBalance = Distribution.SocBalance,
            SocMin = Distribution.SocMin,
            SocMax = Distribution.SocMax
        }
    };
}

public sealed class SlopeConfig
{
    public bool Enabled { get; set; }
    public double RiseKwPerSec { get; set; } = 100;
    public double FallKwPerSec { get; set; } = 100;
}

public sealed class PidConfig
{
    public double Kp { get; set; } = 0.4;
    public double Ki { get; set; } = 0.05;
    public double Kb { get; set; } = 0.5;
    public TimeSpan Period { get; set; } = TimeSpan.FromMilliseconds(3000);
    public double DeadbandKw { get; set; }
    public double OutMinKw { get; set; } = -5000;
    public double OutMaxKw { get; set; } = 5000;
    public PidDiscretization Discretization { get; set; } = PidDiscretization.CompatiblePeriod;
}

public sealed class PrimaryFrequencyConfig
{
    public bool Enabled { get; set; } = true;
    public double RatedFrequencyHz { get; set; } = 50;
    public double Deadband1Percent { get; set; } = 0.2;
    public double Deadband2Percent { get; set; } = 0.5;
    public double DroopPercent { get; set; } = 3;
    public double Droop2Percent { get; set; } = 5;
    public int SegmentCount { get; set; } = 3;
    public bool OverFreqEnable { get; set; } = true;
    public bool UnderFreqEnable { get; set; } = true;
    public TimeSpan ControlCycle { get; set; } = TimeSpan.FromMilliseconds(1000);
    public TimeSpan ResetTime { get; set; } = TimeSpan.FromMilliseconds(2000);
    public double MaxOutputKw { get; set; } = 5000;
    public double MaxAbsorbKw { get; set; } = 5000;
    public double LimitCoefficient { get; set; } = 1;
}

public sealed class DistributionConfig
{
    public bool SocBalance { get; set; }
    public double SocMin { get; set; } = 0.1;
    public double SocMax { get; set; } = 0.9;
}

public sealed class InertiaConfig
{
    public bool Enabled { get; set; }
    public bool LockPrimaryFrequency { get; set; }
    public double RatedFrequencyHz { get; set; } = 50;
    public double InertiaTimeSec { get; set; } = 6;
    public double AmplitudeDeadbandHz { get; set; } = 0.05;
    public double RateDeadbandHzPerSec { get; set; } = 0.1;
    public double FreqMinHz { get; set; } = 47;
    public double FreqMaxHz { get; set; } = 53;
    public TimeSpan ControlCycle { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan ResetTime { get; set; } = TimeSpan.FromMilliseconds(2000);

    public InertiaConfig Clone() => new()
    {
        Enabled = Enabled,
        LockPrimaryFrequency = LockPrimaryFrequency,
        RatedFrequencyHz = RatedFrequencyHz,
        InertiaTimeSec = InertiaTimeSec,
        AmplitudeDeadbandHz = AmplitudeDeadbandHz,
        RateDeadbandHzPerSec = RateDeadbandHzPerSec,
        FreqMinHz = FreqMinHz,
        FreqMaxHz = FreqMaxHz,
        ControlCycle = ControlCycle,
        ResetTime = ResetTime
    };
}

public sealed class VoltageDroopConfig
{
    public bool Enabled { get; set; }
    public double RatedVoltageV { get; set; } = 35000;
    public double Deadband1Percent { get; set; } = 0.5;
    public double Deadband2Percent { get; set; } = 1.5;
    public double K1Percent { get; set; } = 4;
    public double K2Percent { get; set; } = 6;
    public int SegmentCount { get; set; } = 3;
    public bool OverVoltEnable { get; set; } = true;
    public bool UnderVoltEnable { get; set; } = true;
    public TimeSpan ControlCycle { get; set; } = TimeSpan.FromMilliseconds(1000);
    public TimeSpan ResetTime { get; set; } = TimeSpan.FromMilliseconds(2000);
    public double MaxOutputKvar { get; set; } = 5000;
    public double MaxAbsorbKvar { get; set; } = 5000;
    public double LimitCoefficient { get; set; } = 1;

    public VoltageDroopConfig Clone() => new()
    {
        Enabled = Enabled,
        RatedVoltageV = RatedVoltageV,
        Deadband1Percent = Deadband1Percent,
        Deadband2Percent = Deadband2Percent,
        K1Percent = K1Percent,
        K2Percent = K2Percent,
        SegmentCount = SegmentCount,
        OverVoltEnable = OverVoltEnable,
        UnderVoltEnable = UnderVoltEnable,
        ControlCycle = ControlCycle,
        ResetTime = ResetTime,
        MaxOutputKvar = MaxOutputKvar,
        MaxAbsorbKvar = MaxAbsorbKvar,
        LimitCoefficient = LimitCoefficient
    };
}

public sealed class CurvePointConfig
{
    public int? Weekday { get; set; }
    public string? Date { get; set; }
    public string Start { get; set; } = "00:00:00";
    public string End { get; set; } = "24:00:00";
    public double Power { get; set; }
}

public sealed class CurveConfig
{
    public CurveMatchMode Mode { get; set; } = CurveMatchMode.Weekday;
    public List<CurvePointConfig> Points { get; set; } = new();

    public CurveConfig Clone() => new()
    {
        Mode = Mode,
        Points = Points.Select(p => new CurvePointConfig
        {
            Weekday = p.Weekday,
            Date = p.Date,
            Start = p.Start,
            End = p.End,
            Power = p.Power
        }).ToList()
    };
}

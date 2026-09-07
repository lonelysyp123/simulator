using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain.Algorithms;

namespace EssSimulator.EmsStrategy.Domain.Services;

/// <summary>
/// 有功策略：开环不做并网点 PI、不叠加调频/惯量；闭环合成 P_base+ΔP_freq+ΔP_inertia。
/// 惯量 ACTION 且配置闭锁时跳过一次调频。曲线未命中 WAIT，输出 0。
/// </summary>
public sealed class ActivePowerStrategy
{
    private readonly SlopeLimiter _slope = new();
    private readonly PidController _pi = new();
    private readonly AuxActionMachine _pfr = new();
    private readonly AuxActionMachine _inertia = new();
    private readonly InertiaCalculator _inertiaCalc = new();
    private ActiveMode _mode = ActiveMode.CloseLoopFixed;
    private double _lockedPBase;
    private bool _lockArmed;
    private double _lastPi;
    private double _lastPBase;
    private double _lastTarget;
    private double _lastBeforeLimit;
    private double _lastAfterLimit;
    private bool _curveWait;

    public ActionState FrequencyAction => _pfr.State;
    public double FrequencyDeltaKw => _pfr.State == ActionState.Action ? _pfr.Output : 0;
    public ActionState InertiaAction => _inertia.State;
    public double InertiaDeltaKw => _inertia.State == ActionState.Action ? _inertia.Output : 0;
    public bool CurveWait => _curveWait;
    public double LastPiOutput => _lastPi;
    public double LastPBase => _lastPBase;
    public double LastTarget => _lastTarget;
    public double LastBeforeLimit => _lastBeforeLimit;
    public double LastAfterLimit => _lastAfterLimit;

    public void ApplyConfig(EmsStrategyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        _slope.Configure(config.Slope.Enabled, config.Slope.RiseKwPerSec, config.Slope.FallKwPerSec);
        _pi.Configure(config.ActivePid);
    }

    public void Reset()
    {
        _slope.Reset();
        _pi.Reset();
        _pfr.Reset();
        _inertia.Reset();
        _inertiaCalc.Reset();
        _lockArmed = false;
        _lockedPBase = 0;
        _lastPi = 0;
        _lastPBase = 0;
        _lastTarget = 0;
        _lastBeforeLimit = 0;
        _lastAfterLimit = 0;
        _curveWait = false;
    }

    public double Step(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(meas);

        if (_mode != config.ActiveMode)
        {
            Reset();
            _mode = config.ActiveMode;
        }

        ApplyConfig(config);

        if (config.ActiveMode == ActiveMode.OpenLoopFixed)
            return StepOpenLoop(config, meas, dt);

        return StepCloseLoop(config, meas, dt);
    }

    private double StepOpenLoop(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        double set = ResolveFixedSetpoint(config);
        _curveWait = false;
        _lastPBase = set;
        _pfr.Reset();
        _inertia.Reset();
        _lastTarget = set;
        double sloped = _slope.Step(set, dt);
        _lastBeforeLimit = sloped;
        double limited = ApparentPowerLimiter.LimitActive(sloped, meas.PccReactivePowerKvar, config.ApparentRatedKva);
        _lastAfterLimit = limited;
        _lastPi = limited;
        return limited;
    }

    private double StepCloseLoop(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        double curveP = 0;
        if (config.ActiveMode == ActiveMode.CloseLoopCurve
            && !CurveScheduler.TryGetPower(meas.SimTime, config.ActiveCurve, out curveP))
        {
            _curveWait = true;
            _pfr.Reset();
            _inertia.Reset();
            _lastPBase = 0;
            _lastTarget = 0;
            _lastBeforeLimit = 0;
            _lastAfterLimit = 0;
            _lastPi = 0;
            _slope.Reset();
            _pi.Reset();
            return 0;
        }

        _curveWait = false;
        double pBase = config.ActiveMode == ActiveMode.CloseLoopCurve
            ? curveP
            : ResolveFixedSetpoint(config);
        double rated = config.PlantRatedKw > 0 ? config.PlantRatedKw : SumRated(meas);
        var inertiaCfg = config.Inertia ?? new InertiaConfig();
        var pfrCfg = config.PrimaryFrequency;

        double prevFreq = FrequencyDeltaKw;
        double prevInr = InertiaDeltaKw;
        double inertiaDelta = 0;
        if (config.ActiveEnable && inertiaCfg.Enabled)
        {
            var sample = _inertiaCalc.Evaluate(meas.FrequencyHz, dt, rated, inertiaCfg);
            inertiaDelta = _inertia.Step(
                sample.ConditionMet,
                dt,
                inertiaCfg.ControlCycle,
                inertiaCfg.ResetTime,
                () => sample.PowerKw);
            if (_inertia.State != ActionState.Action)
                inertiaDelta = 0;
        }
        else
        {
            _inertia.Reset();
            _inertiaCalc.Reset();
        }

        bool skipPfr = inertiaCfg.LockPrimaryFrequency && _inertia.State == ActionState.Action;
        bool pfrOn = config.ActiveEnable && pfrCfg.Enabled && !skipPfr;
        bool outside = pfrOn && FrequencyDroopCalculator.IsOutsideDeadband(meas.FrequencyHz, pfrCfg);
        double freqDelta = 0;
        if (pfrOn)
        {
            freqDelta = _pfr.Step(
                outside,
                dt,
                pfrCfg.ControlCycle,
                pfrCfg.ResetTime,
                () => FrequencyDroopCalculator.ComputeDeltaKw(meas.FrequencyHz, rated, pfrCfg));
            if (_pfr.State != ActionState.Action)
                freqDelta = 0;
        }
        else
        {
            _pfr.Reset();
        }

        if (_pfr.State == ActionState.Action)
        {
            if (!_lockArmed)
            {
                _lockedPBase = pBase;
                _lockArmed = true;
            }
            else if (IsReverse(pBase - _lockedPBase, freqDelta))
            {
                pBase = _lockedPBase;
            }
        }
        else
        {
            _lockArmed = false;
        }

        _lastPBase = pBase;
        double target = pBase + freqDelta + inertiaDelta;
        _lastTarget = target;

        bool auxJump = Math.Abs(freqDelta - prevFreq) > 1e-6 || Math.Abs(inertiaDelta - prevInr) > 1e-6;
        double sloped = _slope.Step(target, dt);
        _lastBeforeLimit = sloped;
        double limited = ApparentPowerLimiter.LimitActive(sloped, meas.PccReactivePowerKvar, config.ApparentRatedKva);
        _lastAfterLimit = limited;

        double error = limited - meas.PccActivePowerKw;
        _lastPi = _pi.Step(error, dt, force: auxJump);
        return _lastPi;
    }

    private static double ResolveFixedSetpoint(EmsStrategyConfig config) =>
        config.LocalRemote == LocalRemote.Remote ? config.RemoteActiveSetKw : config.LocalActiveSetKw;

    private static bool IsReverse(double scheduleDelta, double freqDelta) =>
        scheduleDelta * freqDelta < -1e-9;

    private static double SumRated(PlantMeasurements meas)
    {
        double s = 0;
        foreach (var b in meas.Branches)
            s += Math.Max(0, b.RatedKw);
        return s > 0 ? s : 1;
    }
}

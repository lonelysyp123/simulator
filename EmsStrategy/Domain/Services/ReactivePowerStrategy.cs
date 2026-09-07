using EssSimulator.EmsStrategy.Application;
using EssSimulator.EmsStrategy.Domain.Algorithms;

namespace EssSimulator.EmsStrategy.Domain.Services;

/// <summary>
/// 无功策略：开环无 PI/下垂；闭环固定/曲线叠加 ΔQ 下垂；PF/恒压生成 Q_base 后走闭环链路。
/// </summary>
public sealed class ReactivePowerStrategy
{
    private readonly SlopeLimiter _slope = new();
    private readonly PidController _pi = new();
    private readonly AuxActionMachine _droop = new();
    private ReactiveMode _mode = ReactiveMode.CloseLoopFixed;
    private double _lastPi;
    private double _lastQBase;
    private double _lastTarget;
    private bool _curveWait;

    public ActionState DroopAction => _droop.State;
    public double DroopDeltaKvar => _droop.State == ActionState.Action ? _droop.Output : 0;
    public bool CurveWait => _curveWait;
    public double LastPiOutput => _lastPi;
    public double LastQBase => _lastQBase;
    public double LastTarget => _lastTarget;

    public void ApplyConfig(EmsStrategyConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var slope = config.ReactiveSlope ?? config.Slope;
        _slope.Configure(slope.Enabled, slope.RiseKwPerSec, slope.FallKwPerSec);
        _pi.Configure(config.ReactivePid ?? config.ActivePid);
    }

    public void Reset()
    {
        _slope.Reset();
        _pi.Reset();
        _droop.Reset();
        _lastPi = 0;
        _lastQBase = 0;
        _lastTarget = 0;
        _curveWait = false;
    }

    public double Step(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(meas);

        if (_mode != config.ReactiveMode)
        {
            Reset();
            _mode = config.ReactiveMode;
        }

        ApplyConfig(config);

        if (config.ReactiveMode == ReactiveMode.OpenLoopFixed)
            return StepOpenLoop(config, meas, dt);

        return StepClosedFamily(config, meas, dt);
    }

    private double StepOpenLoop(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        _curveWait = false;
        _droop.Reset();
        double set = ResolveFixed(config);
        _lastQBase = set;
        _lastTarget = set;
        double sloped = _slope.Step(set, dt);
        double limited = ApparentPowerLimiter.LimitReactive(sloped, meas.PccActivePowerKw, config.ApparentRatedKva);
        _lastPi = limited;
        return limited;
    }

    private double StepClosedFamily(EmsStrategyConfig config, PlantMeasurements meas, TimeSpan dt)
    {
        double curveQ = 0;
        if (config.ReactiveMode == ReactiveMode.CloseLoopCurve
            && !CurveScheduler.TryGetPower(meas.SimTime, config.ReactiveCurve, out curveQ))
        {
            _curveWait = true;
            _droop.Reset();
            _lastQBase = 0;
            _lastTarget = 0;
            _lastPi = 0;
            _slope.Reset();
            _pi.Reset();
            return 0;
        }

        _curveWait = false;
        double qBase = config.ReactiveMode switch
        {
            ReactiveMode.CloseLoopCurve => curveQ,
            ReactiveMode.PowerFactor => PfToReactiveConverter.FromPf(
                meas.PccActivePowerKw, config.PowerFactorSet, config.PfSign),
            ReactiveMode.VoltageFixed => config.VoltageKp * (config.VoltageSetV - meas.PccLineVoltageV),
            _ => ResolveFixed(config)
        };

        double prevDroop = DroopDeltaKvar;
        double droopDelta = 0;
        bool droopOn = config.ReactiveEnable
            && config.VoltageDroop.Enabled
            && config.ReactiveMode is ReactiveMode.CloseLoopFixed or ReactiveMode.CloseLoopCurve;
        var droopCfg = config.VoltageDroop;
        if (droopOn)
        {
            bool outside = VoltageDroopCalculator.IsOutsideDeadband(meas.PccLineVoltageV, droopCfg);
            double qRated = config.PlantRatedKw > 0 ? config.PlantRatedKw : config.ApparentRatedKva;
            droopDelta = _droop.Step(
                outside,
                dt,
                droopCfg.ControlCycle,
                droopCfg.ResetTime,
                () => VoltageDroopCalculator.ComputeDeltaKvar(meas.PccLineVoltageV, qRated, droopCfg));
            if (_droop.State != ActionState.Action)
                droopDelta = 0;
        }
        else
        {
            _droop.Reset();
        }

        _lastQBase = qBase;
        double target = qBase + droopDelta;
        _lastTarget = target;
        bool auxJump = Math.Abs(droopDelta - prevDroop) > 1e-6;
        double sloped = _slope.Step(target, dt);
        double limited = ApparentPowerLimiter.LimitReactive(sloped, meas.PccActivePowerKw, config.ApparentRatedKva);
        double error = limited - meas.PccReactivePowerKvar;
        _lastPi = _pi.Step(error, dt, force: auxJump);
        return _lastPi;
    }

    private static double ResolveFixed(EmsStrategyConfig config) =>
        config.LocalRemote == LocalRemote.Remote ? config.RemoteReactiveSetKvar : config.LocalReactiveSetKvar;
}

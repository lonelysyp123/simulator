using EssSimulator.EmsStrategy.Application;

namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 电站 PI。兼容模式按 Period 采样积分；Dt 模式用实际 dt。
/// force=true 时立即计算（辅助服务首拍），不等完整周期。
/// 离散化：I += Ki * error * stepSec；饱和后 I += Kb * (sat - unsat) * stepSec。
/// </summary>
public sealed class PidController
{
    public double Kp { get; private set; }
    public double Ki { get; private set; }
    public double Kb { get; private set; }
    public TimeSpan Period { get; private set; }
    public double Deadband { get; private set; }
    public double OutMin { get; private set; }
    public double OutMax { get; private set; }
    public PidDiscretization Discretization { get; private set; }
    public double Integral { get; private set; }
    public double Output { get; private set; }

    private TimeSpan _elapsed;

    public void Configure(PidConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        Kp = Math.Max(0, cfg.Kp);
        Ki = Math.Max(0, cfg.Ki);
        Kb = Math.Max(0, cfg.Kb);
        Period = cfg.Period <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(3000) : cfg.Period;
        Deadband = Math.Max(0, cfg.DeadbandKw);
        OutMin = cfg.OutMinKw;
        OutMax = cfg.OutMaxKw < cfg.OutMinKw ? cfg.OutMinKw : cfg.OutMaxKw;
        Discretization = cfg.Discretization;
        Output = Math.Clamp(Output, OutMin, OutMax);
        Integral = Math.Clamp(Integral, OutMin, OutMax);
    }

    public void Reset()
    {
        Integral = 0;
        Output = 0;
        _elapsed = TimeSpan.Zero;
    }

    public double Step(double error, TimeSpan dt, bool force = false)
    {
        if (dt < TimeSpan.Zero)
            dt = TimeSpan.Zero;

        if (Math.Abs(error) <= Deadband)
            error = 0;

        _elapsed += dt;
        bool due = force
            || Discretization == PidDiscretization.Dt
            || _elapsed >= Period;
        if (!due)
            return Output;

        double stepSec = Discretization == PidDiscretization.Dt || force
            ? Math.Max(dt.TotalSeconds, 0)
            : Period.TotalSeconds;
        _elapsed = TimeSpan.Zero;
        if (stepSec <= 0)
            return Output;

        double p = Kp * error;
        double unsat = p + Integral + Ki * error * stepSec;
        double sat = Math.Clamp(unsat, OutMin, OutMax);
        Integral += Ki * error * stepSec + Kb * (sat - unsat) * stepSec;
        Integral = Math.Clamp(Integral, OutMin, OutMax);
        Output = sat;
        return Output;
    }
}

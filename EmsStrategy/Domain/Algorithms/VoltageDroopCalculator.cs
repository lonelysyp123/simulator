using EssSimulator.EmsStrategy.Application;

namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 下垂调压。ΔQ = q_start + (u_span − u) / u0 × Q_rated / droop × 100，
/// k1/k2 对应内/外段 droop 百分数。状态机复用 <see cref="AuxActionMachine"/>。
/// </summary>
public static class VoltageDroopCalculator
{
    public static double DeadbandV(VoltageDroopConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        return Math.Abs(cfg.RatedVoltageV) * Math.Max(0, cfg.Deadband1Percent) / 100.0;
    }

    public static double OuterDeadbandV(VoltageDroopConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        double inner = DeadbandV(cfg);
        double outer = Math.Abs(cfg.RatedVoltageV) * Math.Max(0, cfg.Deadband2Percent) / 100.0;
        return Math.Max(outer, inner);
    }

    public static bool IsOutsideDeadband(double voltageV, VoltageDroopConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        double u0 = cfg.RatedVoltageV;
        double db = DeadbandV(cfg);
        if (voltageV < u0 - db)
            return cfg.UnderVoltEnable;
        if (voltageV > u0 + db)
            return cfg.OverVoltEnable;
        return false;
    }

    public static double ComputeDeltaKvar(double voltageV, double plantRatedKvar, VoltageDroopConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        if (!IsOutsideDeadband(voltageV, cfg))
            return 0;

        double u0 = cfg.RatedVoltageV <= 0 ? 35000 : cfg.RatedVoltageV;
        double qRated = plantRatedKvar > 0 ? plantRatedKvar : 1;
        double db1 = DeadbandV(cfg);
        bool under = voltageV < u0;
        double uSpan = under ? u0 - db1 : u0 + db1;
        double qStart = 0;
        double droop = Math.Max(1e-6, cfg.K1Percent);

        if (cfg.SegmentCount >= 5)
        {
            double db2 = OuterDeadbandV(cfg);
            double uBreak = under ? u0 - db2 : u0 + db2;
            bool outer = under ? voltageV < u0 - db2 : voltageV > u0 + db2;
            if (outer)
            {
                qStart = SegmentDelta(uSpan, uBreak, u0, qRated, droop);
                uSpan = uBreak;
                droop = Math.Max(1e-6, cfg.K2Percent);
            }
        }

        double raw = qStart + SegmentDelta(uSpan, voltageV, u0, qRated, droop);
        double coef = Math.Clamp(cfg.LimitCoefficient, 0, 10);
        double maxOut = Math.Max(0, cfg.MaxOutputKvar) * coef;
        double maxAbs = Math.Max(0, cfg.MaxAbsorbKvar) * coef;
        return Math.Clamp(raw, -maxAbs, maxOut);
    }

    private static double SegmentDelta(double uSpan, double u, double u0, double qRated, double droopPercent) =>
        (uSpan - u) / u0 * qRated / droopPercent * 100.0;
}

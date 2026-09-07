using EssSimulator.EmsStrategy.Application;

namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 下垂调压，复刻 <c>ReactivePowerCtrl_QV_Droop</c>：
/// ΔQ = q_start + k · (U_span − U)，k 为 kvar/V（配置字段名仍是 K1Percent/K2Percent）。
/// VoltageCurveType=0 从死区边沿起算；=1 从额定电压起算。
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
        double db1 = DeadbandV(cfg);
        double db2 = OuterDeadbandV(cfg);
        bool five = cfg.SegmentCount >= 5;
        bool fromRated = cfg.VoltageCurveType != 0;
        bool outer = voltageV < u0 - db2 || voltageV > u0 + db2;
        double k = five && outer ? cfg.K2Percent : cfg.K1Percent;
        double qStart = 0;
        double uSpan = u0;

        if (five)
        {
            if (!fromRated && voltageV < u0 - db1 && voltageV >= u0 - db2)
                uSpan = u0 - db1;
            if (!fromRated && voltageV > u0 + db1 && voltageV <= u0 + db2)
                uSpan = u0 + db1;
            if (voltageV < u0 - db2)
            {
                uSpan = u0 - db2;
                qStart = fromRated ? cfg.K1Percent * db2 : cfg.K1Percent * (db2 - db1);
            }
            if (voltageV > u0 + db2)
            {
                uSpan = u0 + db2;
                qStart = fromRated ? -cfg.K1Percent * db2 : -cfg.K1Percent * (db2 - db1);
            }
        }
        else
        {
            if (voltageV < u0 - db1 && !fromRated)
                uSpan = u0 - db1;
            if (voltageV > u0 + db1 && !fromRated)
                uSpan = u0 + db1;
        }

        double raw = qStart + k * (uSpan - voltageV);
        double coef = Math.Clamp(cfg.LimitCoefficient, 0, 10);
        double maxOut = Math.Max(0, cfg.MaxOutputKvar) * coef;
        double maxAbs = Math.Max(0, cfg.MaxAbsorbKvar) * coef;
        if (raw < -0.01)
            return -Math.Min(-raw, maxAbs);
        return Math.Min(raw, maxOut);
    }
}

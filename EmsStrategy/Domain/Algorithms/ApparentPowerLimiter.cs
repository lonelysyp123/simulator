namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 视在功率限幅：用另一轴当前值限制本轴，使 √(P²+Q²) ≤ S。
/// </summary>
public static class ApparentPowerLimiter
{
    public static double LimitActive(double activeKw, double otherAxisKvar, double apparentRatedKva)
    {
        if (apparentRatedKva <= 0)
            return 0;
        double q = otherAxisKvar;
        double s2 = apparentRatedKva * apparentRatedKva;
        double q2 = q * q;
        if (q2 >= s2)
            return 0;
        double pMax = Math.Sqrt(s2 - q2);
        return Math.Clamp(activeKw, -pMax, pMax);
    }

    public static double LimitReactive(double reactiveKvar, double otherAxisKw, double apparentRatedKva)
        => LimitActive(reactiveKvar, otherAxisKw, apparentRatedKva);
}

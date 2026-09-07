namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 斜率限制：按升/降速率限制目标相对当前值的变化。
/// 关闭时输出等于设定。过零时先按下降速率靠近 0，不会一步变号到位。
/// </summary>
public sealed class SlopeLimiter
{
    public bool Enabled { get; private set; }
    public double RiseKwPerSec { get; private set; }
    public double FallKwPerSec { get; private set; }
    public double Current { get; private set; }

    public void Configure(bool enabled, double riseKwPerSec, double fallKwPerSec)
    {
        Enabled = enabled;
        RiseKwPerSec = Math.Max(0, riseKwPerSec);
        FallKwPerSec = Math.Max(0, fallKwPerSec);
    }

    public void Reset(double value = 0) => Current = value;

    public double Step(double target, TimeSpan dt)
    {
        if (!Enabled || dt <= TimeSpan.Zero)
        {
            Current = target;
            return Current;
        }

        double seconds = dt.TotalSeconds;
        double rise = RiseKwPerSec * seconds;
        double fall = FallKwPerSec * seconds;
        double next = Advance(Current, target, rise, fall);
        Current = next;
        return Current;
    }

    internal static double Advance(double current, double target, double riseStep, double fallStep)
    {
        if (target > current)
            return Math.Min(target, current + Math.Max(0, riseStep));
        if (target < current)
            return Math.Max(target, current - Math.Max(0, fallStep));
        return target;
    }
}

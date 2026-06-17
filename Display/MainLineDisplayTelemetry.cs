using System.Diagnostics;

namespace EssSimulator.Display
{
    /// <summary>记录主接线图每次绘制时的指标，用于 UI 帧级延迟统计。</summary>
    public static class MainLineDisplayTelemetry
    {
        private static readonly object Gate = new();
        private static readonly Dictionary<string, double> LastMetricValues = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, long> LastMetricChangeTicks = new(StringComparer.OrdinalIgnoreCase);

        public static long LastFrameTicks { get; private set; }
        public static string? LastRenderDigest { get; private set; }

        public static void RecordFrame(
            string rendered,
            MainLineSnapshot snap,
            int unitStart,
            int unitEndExclusive)
        {
            long now = Stopwatch.GetTimestamp();
            lock (Gate)
            {
                LastFrameTicks = now;
                LastRenderDigest = rendered.Length > 200 ? rendered[..200] : rendered;

                for (int u = unitStart; u < unitEndExclusive; u++)
                {
                    TouchMetric($"unit.{u}.breaker.closed", snap, snap);
                    TouchMetric($"unit.{u}.pcsA.activePowerKw", snap, snap);
                    TouchMetric($"unit.{u}.pcsB.activePowerKw", snap, snap);
                    TouchMetric($"unit.{u}.bmsA.soc", snap, snap);
                    TouchMetric($"unit.{u}.bmsB.soc", snap, snap);
                }

                TouchMetric("mainBreaker.closed", snap, snap);
                TouchMetric("load.activePowerKw", snap, snap);
                TouchMetric("load.reactivePowerKvar", snap, snap);
            }
        }

        private static void TouchMetric(string key, MainLineSnapshot snap, MainLineSnapshot _)
        {
            if (!MainLineMetrics.TryRead(key, snap, out var val))
                return;

            if (!LastMetricValues.TryGetValue(key, out var prev) || Math.Abs(prev - val) > 1e-9)
            {
                LastMetricValues[key] = val;
                LastMetricChangeTicks[key] = Stopwatch.GetTimestamp();
            }
        }

        public static bool TryGetMetricChangeTicks(string metric, out long ticks)
        {
            lock (Gate)
            {
                return LastMetricChangeTicks.TryGetValue(metric, out ticks);
            }
        }

        public static void ResetMetric(string metric)
        {
            lock (Gate)
            {
                LastMetricValues.Remove(metric);
                LastMetricChangeTicks.Remove(metric);
            }
        }

        public static double TicksToMs(long fromTicks, long toTicks) =>
            (toTicks - fromTicks) * 1000.0 / Stopwatch.Frequency;
    }
}

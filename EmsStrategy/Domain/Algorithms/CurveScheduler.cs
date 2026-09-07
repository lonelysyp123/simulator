using EssSimulator.EmsStrategy.Application;

namespace EssSimulator.EmsStrategy.Domain.Algorithms;

/// <summary>
/// 计划曲线：星期模式或日期模式匹配当前时刻功率。未命中返回 false（WAIT）。
/// </summary>
public static class CurveScheduler
{
    public static bool TryGetPower(DateTime simTime, CurveConfig? cfg, out double powerKw)
    {
        powerKw = 0;
        if (cfg?.Points == null || cfg.Points.Count == 0)
            return false;

        var tod = simTime.TimeOfDay;
        foreach (var p in cfg.Points)
        {
            if (!TimeMatches(tod, p.Start, p.End))
                continue;
            if (cfg.Mode == CurveMatchMode.Weekday)
            {
                int wd = p.Weekday ?? -1;
                if (wd == (int)simTime.DayOfWeek)
                {
                    powerKw = p.Power;
                    return true;
                }
            }
            else if (!string.IsNullOrWhiteSpace(p.Date)
                     && DateOnly.TryParse(p.Date, out var d)
                     && d == DateOnly.FromDateTime(simTime))
            {
                powerKw = p.Power;
                return true;
            }
        }

        return false;
    }

    private static bool TimeMatches(TimeSpan tod, string? startText, string? endText)
    {
        if (!TimeSpan.TryParse(startText, out var start))
            start = TimeSpan.Zero;
        if (!TimeSpan.TryParse(endText, out var end))
            end = new TimeSpan(23, 59, 59);
        if (end <= start)
            return tod >= start || tod < end;
        return tod >= start && tod < end;
    }
}

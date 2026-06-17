using System.Globalization;

namespace EssSimulator.Display
{
    /// <summary>从 <see cref="MainLineSnapshot"/> 读取主接线图可显示量。</summary>
    public static class MainLineMetrics
    {
        public static bool TryRead(string metric, MainLineSnapshot snap, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(metric))
                return false;

            var parts = metric.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return false;

            if (parts[0].Equals("mainBreaker", StringComparison.OrdinalIgnoreCase) &&
                parts.Length >= 2 &&
                parts[1].Equals("closed", StringComparison.OrdinalIgnoreCase))
            {
                value = snap.MainBreakerClosed ? 1 : 0;
                return true;
            }

            if (parts[0].Equals("load", StringComparison.OrdinalIgnoreCase) && parts.Length >= 2)
            {
                if (parts[1].Equals("activePowerKw", StringComparison.OrdinalIgnoreCase))
                {
                    value = snap.LoadActivePowerKw;
                    return true;
                }
                if (parts[1].Equals("reactivePowerKvar", StringComparison.OrdinalIgnoreCase))
                {
                    value = snap.LoadReactivePowerKvar;
                    return true;
                }
            }

            if (parts[0].Equals("unit", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3 &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int unitIndex))
            {
                var unit = snap.Units.FirstOrDefault(u => u.UnitIndex == unitIndex);
                if (unit == null)
                    return false;

                if (parts[2].Equals("breaker", StringComparison.OrdinalIgnoreCase) &&
                    parts.Length >= 4 &&
                    parts[3].Equals("closed", StringComparison.OrdinalIgnoreCase))
                {
                    value = unit.UnitBreakerClosed ? 1 : 0;
                    return true;
                }

                if (parts[2].Equals("pcsA", StringComparison.OrdinalIgnoreCase) &&
                    parts.Length >= 4 &&
                    parts[3].Equals("activePowerKw", StringComparison.OrdinalIgnoreCase) &&
                    unit.PcsA != null)
                {
                    value = unit.PcsA.Value.ActivePowerKw;
                    return true;
                }

                if (parts[2].Equals("pcsB", StringComparison.OrdinalIgnoreCase) &&
                    parts.Length >= 4 &&
                    parts[3].Equals("activePowerKw", StringComparison.OrdinalIgnoreCase) &&
                    unit.PcsB != null)
                {
                    value = unit.PcsB.Value.ActivePowerKw;
                    return true;
                }

                if (parts[2].Equals("bmsA", StringComparison.OrdinalIgnoreCase) &&
                    parts.Length >= 4 &&
                    parts[3].Equals("soc", StringComparison.OrdinalIgnoreCase))
                {
                    int bmsIndex = unit.UnitIndex * 2;
                    value = 100 * GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{bmsIndex}]._currentState.MinClusterSOC");
                    return true;
                }

                if (parts[2].Equals("bmsB", StringComparison.OrdinalIgnoreCase) &&
                    parts.Length >= 4 &&
                    parts[3].Equals("soc", StringComparison.OrdinalIgnoreCase))
                {
                    int bmsIndex = unit.UnitIndex * 2 + 1;
                    value = 100 * GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{bmsIndex}]._currentState.MinClusterSOC");
                    return true;
                }
            }

            return false;
        }

        public static bool Changed(double before, double after, double tolerance) =>
            Math.Abs(before - after) > tolerance;

        public static void PrintCatalog()
        {
            Console.WriteLine("主接线可观测 metric（用于 perftest drive / observe.snapshot）:");
            Console.WriteLine("  mainBreaker.closed");
            Console.WriteLine("  load.activePowerKw | load.reactivePowerKvar");
            Console.WriteLine("  unit.{N}.breaker.closed");
            Console.WriteLine("  unit.{N}.pcsA.activePowerKw | unit.{N}.pcsB.activePowerKw");
            Console.WriteLine("  unit.{N}.bmsA.soc | unit.{N}.bmsB.soc   (N=0,1,…)");
        }
    }
}

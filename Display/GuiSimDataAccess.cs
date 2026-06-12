using log4net;

namespace EssSimulator.Display
{
    /// <summary>GUI 读取仿真对象路径的辅助方法。</summary>
    internal static class GuiSimDataAccess
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GuiSimDataAccess));

        public static int GetEssUnitCount()
        {
            try
            {
                var list = SimServer.GetExtIfVariableVal("ess._pcsList");
                return list is System.Collections.ICollection c ? c.Count : 0;
            }
            catch (Exception ex)
            {
                Log.Debug("GetEssUnitCount 失败，回退 2", ex);
                return 2;
            }
        }

        public static int GetMainLineSectionCount(int unitsPerSection)
        {
            int channelCount = Math.Max(1, GetEssUnitCount());
            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));
            return Math.Max(1, (int)Math.Ceiling(unitCount / (double)Math.Max(1, unitsPerSection)));
        }

        public static int ClampMainLineSectionIndex(int requestedSectionIndex, int unitsPerSection)
        {
            int sectionCount = GetMainLineSectionCount(unitsPerSection);
            return Math.Clamp(requestedSectionIndex, 0, sectionCount - 1);
        }

        /// <summary>从 bms1 数据模型读取簇数量，失败时回退 12。</summary>
        public static int GetClusterCount()
        {
            try
            {
                var v = SimServer.GetExtIfVariableVal("bms1.BatteryStacks[0].Cluseter.Count");
                if (v is int i) return Math.Max(1, i);
                if (v is long l) return (int)Math.Max(1, Math.Min(int.MaxValue, l));
                if (v != null && int.TryParse(v.ToString(), out int p)) return Math.Max(1, p);
            }
            catch (Exception ex)
            {
                Log.Debug("GetClusterCount 失败，回退 12", ex);
            }
            return 12;
        }

        public static double SafeGetDouble(string path, double fallback = 0)
        {
            try
            {
                var o = SimServer.GetExtIfVariableVal(path);
                if (o == null) return fallback;
                return Convert.ToDouble(o);
            }
            catch (Exception ex)
            {
                Log.Debug($"SafeGetDouble 失败: {path}", ex);
                return fallback;
            }
        }

        public static bool SafeGetBool(string path, bool fallback = false)
        {
            try
            {
                var o = SimServer.GetExtIfVariableVal(path);
                if (o == null) return fallback;
                return Convert.ToBoolean(o);
            }
            catch (Exception ex)
            {
                Log.Debug($"SafeGetBool 失败: {path}", ex);
                return fallback;
            }
        }
    }
}

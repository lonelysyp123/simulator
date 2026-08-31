namespace EssSimulator.EssDeviceSimModel.Model
{
    /// <summary>径向运行时母线 Id，组态抽头与求解器共用。</summary>
    public static class RuntimeBusIds
    {
        public const string Grid = "BUS_GRID";
        public const string AfterMainBreaker = "BUS_AFTER_MAIN_BRK";
        public const string Station35 = "BUS_35";

        /// <summary>历史径向图 Id，等价于 <see cref="AfterMainBreaker"/>。</summary>
        public const string LegacyAfterMainBreaker = "BUS_MAIN_SEC";

        public static string Unit690(int unitIndex) => $"BUS_690_U{unitIndex}";

        /// <summary>把历史别名收成规范 Id；空或未知 Id 原样返回。</summary>
        public static string Canonicalize(string? busId)
        {
            if (string.IsNullOrWhiteSpace(busId))
                return busId ?? string.Empty;
            if (string.Equals(busId, LegacyAfterMainBreaker, StringComparison.Ordinal))
                return AfterMainBreaker;
            return busId;
        }

        public static bool TryParseUnit690(string busId, out int unitIndex)
        {
            unitIndex = -1;
            const string prefix = "BUS_690_U";
            if (string.IsNullOrWhiteSpace(busId) || !busId.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            return int.TryParse(busId.AsSpan(prefix.Length), out unitIndex) && unitIndex >= 0;
        }
    }
}

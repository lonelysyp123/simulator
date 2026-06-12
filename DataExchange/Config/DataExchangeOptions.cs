using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.DataExchange.Config
{
    public class DataExchangeOptions
    {
        public const string Section = "DataExchange";

        public int TelemetryIntervalMs { get; set; } = 500;
        public int ControlPollIntervalMs { get; set; } = 100;

        public Dictionary<string, string> ControlSemantics { get; set; } = new();
        public Dictionary<string, string> ControlEffects { get; set; } = new();
    }

    public static class DataExchangeOptionParser
    {
        public static ControlSemantics ParseSemantics(string? value, ControlSemantics fallback) =>
            Enum.TryParse<ControlSemantics>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

        public static ControlEffectId ParseEffect(string? value, ControlEffectId fallback) =>
            Enum.TryParse<ControlEffectId>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }
}

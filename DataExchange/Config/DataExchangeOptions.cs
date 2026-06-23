using EssSimulator.DataExchange.Catalog;

namespace EssSimulator.DataExchange.Config
{
    public class DataExchangeOptions
    {
        public const string Section = "DataExchange";

        /// <summary>BMS / rack 遥测刷新周期（ms）。</summary>
        public int TelemetryIntervalMs { get; set; } = 500;

        /// <summary>EMU（PCS）遥测刷新周期（ms）。</summary>
        public int EmuTelemetryIntervalMs { get; set; } = 100;

        /// <summary>Modbus 外部写控制区后是否立即触发控制管道（轮询仍作兜底）。</summary>
        public bool ControlEventDriven { get; set; } = true;

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

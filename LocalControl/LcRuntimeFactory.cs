using EssSimulator.Protocol.Modbus;
using log4net;

namespace EssSimulator.LocalControl
{
    /// <summary>按 LC 型号选型创建运行时：standard 点名桥接，中压表走 ModelSim/DataExchange。</summary>
    internal static class LcRuntimeFactory
    {
        public const string StandardModelId = "standard";
        public const string Trina55ModelId = "trina_5.5MW";
        public const string Trina10ModelId = "trina_10MW";

        public static LcRuntimeBase Create(string? lcModelId, ILog? log = null)
        {
            log ??= LogManager.GetLogger(typeof(LcRuntimeFactory));
            if (string.Equals(lcModelId, Trina55ModelId, StringComparison.OrdinalIgnoreCase))
                return new Trina55MwLcRuntime(log);
            if (string.Equals(lcModelId, Trina10ModelId, StringComparison.OrdinalIgnoreCase))
                return new Trina10MwLcRuntime(log);
            return new StandardLcRuntime(log);
        }

        /// <summary>当前 device-models 选型中的 LC 型号；未选则 standard。</summary>
        public static string ResolveSelectedModelId()
        {
            var selection = DeviceModelRegistry.LoadSelection();
            if (selection.Selections.TryGetValue("lc", out var id) && !string.IsNullOrWhiteSpace(id))
                return id;
            return StandardModelId;
        }
    }
}

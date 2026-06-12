using EssSimulator.DataExchange.Catalog;
using EssSimulator.EssSimModelApi.Bms;

namespace EssSimulator.DataExchange.Effects
{
    /// <summary>BMS 控制点（param11/12）变更后立即驱动并网/黑启动状态机。</summary>
    public sealed class BmsLinkControlEffect : IControlEffect
    {
        public ControlEffectId Id => ControlEffectId.BmsApplyLinkCommands;

        public void OnControlChanged(ControlEffectContext context)
        {
            if (!TryParseBmsChannel(context.ServerName, out int bmsIndex))
                return;

            BmsLinkEngine.ApplyForChannel(bmsIndex);
        }

        internal static bool TryParseBmsChannel(string serverName, out int bmsIndex0)
        {
            bmsIndex0 = 0;
            if (string.IsNullOrWhiteSpace(serverName) ||
                !serverName.StartsWith("simBms", StringComparison.OrdinalIgnoreCase))
                return false;

            return int.TryParse(serverName.AsSpan(6), out int bms1Based) &&
                   bms1Based >= 1 &&
                   (bmsIndex0 = bms1Based - 1) >= 0;
        }
    }
}

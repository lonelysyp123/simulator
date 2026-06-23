using EssSimulator.Core;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;

namespace EssSimulator.DataExchange.Effects
{
    /// <summary>EMU 控制点变更后立即驱动 PCS / 单元断路器逻辑。</summary>
    public sealed class EmuPcsControlEffect : IControlEffect
    {
        private readonly Action? _refreshTelemetry;

        public EmuPcsControlEffect(Action? refreshTelemetry = null) =>
            _refreshTelemetry = refreshTelemetry;

        public ControlEffectId Id => ControlEffectId.PcsApplyCommands;

        public void OnControlChanged(ControlEffectContext context)
        {
            if (!TryParseEmuUnit(context.ServerName, out int unit1Based))
                return;

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{unit1Based}");
            if (ess == null || emu == null)
                return;

            int pcsBase = (unit1Based - 1) * 2;
            PcsMapper.ApplyEmuCommands(emu, ess, pcsBase);
            PcsEmuSynchronizer.SyncPcsStateFromModel(ess, emu, pcsBase);
            _refreshTelemetry?.Invoke();
        }

        internal static bool TryParseEmuUnit(string serverName, out int unit1Based)
        {
            unit1Based = 0;
            if (string.IsNullOrWhiteSpace(serverName) ||
                !serverName.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase))
                return false;

            return int.TryParse(serverName.AsSpan(6), out unit1Based) && unit1Based >= 1;
        }
    }

    /// <summary>EMU 单元高压断路器（emu.Emu.PowerOnOff → 电气网络单元断路器）。</summary>
    public sealed class EmuUnitBreakerEffect : IControlEffect
    {
        public ControlEffectId Id => ControlEffectId.UnitHighVoltageBreaker;

        public void OnControlChanged(ControlEffectContext context)
        {
            if (!EmuPcsControlEffect.TryParseEmuUnit(context.ServerName, out int unit1Based))
                return;

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{unit1Based}");
            if (ess == null || emu == null)
                return;

            ess.SetUnitBreakerClosed(unit1Based - 1, emu.Emu.PowerOnOff != 0);
        }
    }
}

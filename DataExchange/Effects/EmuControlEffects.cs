using EssSimulator.Core;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using EssSimulator.Web;

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
            if (!TryResolveEmuUnit(context, out int unit1Based))
                return;

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{unit1Based}");
            if (ess == null || emu == null)
                return;

            int pcsBase = ess.PcsBaseIndexOfUnit(unit1Based - 1);
            // 先跑 EMU 级系统操作/黑启动写入与功率均分，再下发到各 PCS
            EmuSystemOperationApplier.Apply(emu);
            EmuPowerDispatcher.Dispatch(emu);
            PcsMapper.ApplyEmuCommands(emu, ess, pcsBase);
            PcsEmuSynchronizer.SyncPcsStateFromModel(ess, emu, pcsBase);
            _refreshTelemetry?.Invoke();
            SnapshotService.RequestImmediatePush();
        }

        /// <summary>
        /// 解析控制点作用的机组号：simEmu{n} 直接取设备号；
        /// simLc{n} 等其它设备从绑定目标根路径（emu{n}）取首机组号。
        /// </summary>
        internal static bool TryResolveEmuUnit(ControlEffectContext context, out int unit1Based)
        {
            if (TryParseEmuUnit(context.ServerName, out unit1Based))
                return true;

            return TryParseEmuRoot(context.Binding.Target.RootKey, out unit1Based);
        }

        internal static bool TryParseEmuRoot(string? rootKey, out int unit1Based)
        {
            unit1Based = 0;
            if (string.IsNullOrWhiteSpace(rootKey) ||
                !rootKey.StartsWith("emu", StringComparison.OrdinalIgnoreCase))
                return false;

            return int.TryParse(rootKey.AsSpan(3), out unit1Based) && unit1Based >= 1;
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
            if (!EmuPcsControlEffect.TryResolveEmuUnit(context, out int unit1Based))
                return;

            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{unit1Based}");
            if (ess == null || emu == null)
                return;

            ess.SetUnitBreakerClosed(unit1Based - 1, emu.Emu.PowerOnOff != 0);
            SnapshotService.RequestImmediatePush();
        }
    }
}

using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>ESS 物理模型 ↔ EMU/PCS DTO 单向同步（无 Modbus 依赖）。</summary>
    public static class PcsEmuSynchronizer
    {
        public static void SyncUnit(
            EnergyStorageSystem ess,
            EnergyManagementData emu,
            int unitIndex0,
            int pcsBaseIndex,
            double unitXfRatedKw = 0)
        {
            SyncPcsStateFromModel(ess, emu, pcsBaseIndex);

            int pcsCount = emu.PcsList.Count;
            var racks = new List<BatteryRackSimulator>();
            for (int i = 0; i < pcsCount; i++)
            {
                int rackIdx = pcsBaseIndex + i;
                if (rackIdx < ess._batteryRacks.Count)
                    racks.Add(ess._batteryRacks[rackIdx]);
            }
            PcsMapper.MapEmuState(emu, racks);
            PcsMapper.MapGroupState(emu);

            if (unitIndex0 < ess._unitBreakers.Count)
                ess.SetUnitBreakerClosed(unitIndex0, emu.Emu.PowerOnOff != 0);

            EmuPowerDispatcher.Dispatch(emu);
            EmuSystemOperationApplier.Apply(emu);
            PcsMapper.ApplyEmuCommands(emu, ess, pcsBaseIndex);

            SyncDeviceMirrors(ess, emu, unitIndex0, unitXfRatedKw);
        }

        /// <summary>
        /// 设备镜像刷新：EMU 级断路器状态跟随 Emu.PowerOnOff；
        /// 单元变镜像优先抄电气层真实状态，缺失时用 PCS 求和合成。
        /// </summary>
        private static void SyncDeviceMirrors(
            EnergyStorageSystem ess,
            EnergyManagementData emu,
            int unitIndex0,
            double unitXfRatedKw)
        {
            emu.Breaker.Closed = (ushort)(emu.Emu.PowerOnOff != 0 ? 1 : 0);

            if (emu.Transformers.Count == 0)
                return;
            var xf = emu.Transformers[0];
            xf.Closed = emu.Breaker.Closed;

            if (unitIndex0 < ess._unitTransformers.Count)
            {
                var state = ess._unitTransformers[unitIndex0].GetCurrentState();
                xf.ActivePowerKw = (float)state.Power;
                xf.LoadFraction = (float)Math.Min(1.5, state.LoadRatio);
                xf.OilTemperatureC = (float)state.Temperature;
                if (state.PowerFactor is > 0.05 and <= 1 && Math.Abs(state.Power) > 0.1)
                {
                    double s = Math.Abs(state.Power) / state.PowerFactor;
                    xf.ReactivePowerKvar = (float)Math.Sqrt(Math.Max(0, s * s - state.Power * state.Power));
                }
                else
                {
                    xf.ReactivePowerKvar = 0f;
                }
            }
            else if (unitXfRatedKw > 0)
            {
                xf.ActivePowerKw = emu.Emu.OutputActivePower;
                xf.ReactivePowerKvar = emu.Emu.OutputReactivePower;
                xf.LoadFraction = Math.Min(1.5f, Math.Abs(emu.Emu.OutputActivePower) / (float)unitXfRatedKw);
            }

            xf.OperationStatus = xf.Closed == 0 ? (ushort)1
                : Math.Abs(xf.ActivePowerKw) > 1 ? (ushort)3 : (ushort)2;
        }

        /// <summary>控制命令后立即把物理 PCS 状态映射到 DTO（不重复 ApplyEmuCommands）。</summary>
        public static void SyncPcsStateFromModel(
            EnergyStorageSystem ess,
            EnergyManagementData emu,
            int pcsBaseIndex)
        {
            int pcsCount = emu.PcsList.Count;
            if (pcsCount == 0 ||
                pcsBaseIndex + pcsCount > ess._pcsList.Count ||
                pcsBaseIndex + pcsCount > ess._batteryRacks.Count)
                return;

            for (int i = 0; i < pcsCount; i++)
            {
                PcsMapper.MapPcsState(
                    ess._pcsList[pcsBaseIndex + i].GetCurrentState(),
                    emu.PcsList[i],
                    ess._batteryRacks[pcsBaseIndex + i]);
                PcsMapper.SyncRunCommandFeedback(ess._pcsList[pcsBaseIndex + i], emu.PcsList[i]);
            }
        }
    }
}

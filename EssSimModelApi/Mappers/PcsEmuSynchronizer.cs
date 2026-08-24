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
            int pcsBaseIndex)
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

            if (unitIndex0 < ess._unitBreakers.Count)
                ess.SetUnitBreakerClosed(unitIndex0, emu.Emu.PowerOnOff != 0);

            EmuPowerDispatcher.Dispatch(emu);
            EmuSystemOperationApplier.Apply(emu);
            PcsMapper.ApplyEmuCommands(emu, ess, pcsBaseIndex);
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

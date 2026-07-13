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
            PcsMapper.MapEmuState(emu, new[] { ess._batteryRacks[pcsBaseIndex], ess._batteryRacks[pcsBaseIndex + 1] });

            if (unitIndex0 < ess._unitBreakers.Count)
                ess.SetUnitBreakerClosed(unitIndex0, emu.Emu.PowerOnOff != 0);

            PcsMapper.ApplyEmuCommands(emu, ess, pcsBaseIndex);
        }

        /// <summary>控制命令后立即把物理 PCS 状态映射到 DTO（不重复 ApplyEmuCommands）。</summary>
        public static void SyncPcsStateFromModel(
            EnergyStorageSystem ess,
            EnergyManagementData emu,
            int pcsBaseIndex)
        {
            if (pcsBaseIndex + 1 >= ess._pcsList.Count || pcsBaseIndex + 1 >= ess._batteryRacks.Count)
                return;

            PcsMapper.MapPcsState(
                ess._pcsList[pcsBaseIndex].GetCurrentState(),
                emu.PcsList[0],
                ess._batteryRacks[pcsBaseIndex]);
            PcsMapper.MapPcsState(
                ess._pcsList[pcsBaseIndex + 1].GetCurrentState(),
                emu.PcsList[1],
                ess._batteryRacks[pcsBaseIndex + 1]);

            PcsMapper.SyncRunCommandFeedback(ess._pcsList[pcsBaseIndex], emu.PcsList[0]);
            PcsMapper.SyncRunCommandFeedback(ess._pcsList[pcsBaseIndex + 1], emu.PcsList[1]);
        }
    }
}

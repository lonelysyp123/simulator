using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using static EssSimulator.EssDeviceSimModel.EnergyStorageSystem;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>
    /// 将 ESS 物理模型数据映射到 PCS/EMU 接口数据对象，以及将 EMU 控制命令回写到模型。
    /// </summary>
    public static class PcsMapper
    {
        /// <summary>将一路 PCS 物理状态同步到 PcsData DTO。</summary>
        public static void MapPcsState(PcsState src, PcsData dst, BatteryRackSimulator bms)
        {
            dst.LineVoltageAB = (float)src.AcVoltage;
            dst.LineVoltageBC = (float)src.AcVoltage;
            dst.LineVoltageCA = (float)src.AcVoltage;
            dst.Frequency     = (float)src.Frequency;
            dst.PhaseACurrent = (float)src.AcCurrent;
            dst.PhaseBCurrent = (float)src.AcCurrent;
            dst.PhaseCCurrent = (float)src.AcCurrent;

            dst.BatteryVoltage = (float)src.DcVoltage;
            dst.BatteryCurrent = (float)src.DcCurrent;
            dst.BatteryPower   = (float)src.DcVoltage * (float)src.DcCurrent;

            dst.ActivePower        = (float)src.ActivePower;
            dst.ReactivePower      = (float)src.ReactivePower;
            dst.AvailableCapacity  = 100;

            double denom = Math.Sqrt(Math.Pow(src.ActivePower, 2) + Math.Pow(src.ReactivePower, 2));
            dst.PowerFactor = denom > 0 ? (float)(src.ActivePower / denom) : 0f;

            dst.TotalChargeEnergy    = (float)src.TotalChargeEnergy;
            dst.TotalDischargeEnergy = (float)src.TotalDischargeEnergy;
            dst.DailyChargeEnergy    = (float)src.DailyChargeEnergy;
            dst.DailyDischargeEnergy = (float)src.DailyDischargeEnergy;
        }

        /// <summary>更新 EMU 汇总数据（运行状态、SOC 等）。</summary>
        public static void MapEmuState(EnergyManagementData emu, BatteryRackSimulator bms1)
        {
            var pcs0 = emu.PcsList[0];
            var pcs1 = emu.PcsList[1];

            emu.Emu.MaxChargePower    = 1250.0f;
            emu.Emu.MaxDischargePower = 1250.0f;
            emu.Emu.AverageBatterySoc = (float)bms1.GetRackState().MinClusterSOC * 100;

            // 运行状态判断（正放负充：ActivePower>0为放电，<0为充电）
            if (pcs0.ActivePower > 10 || pcs1.ActivePower > 10)
                emu.Emu.OperationStatus = 4;  // 放电
            else if (pcs0.ActivePower < -10 || pcs1.ActivePower < -10)
                emu.Emu.OperationStatus = 3;  // 充电
            else
                emu.Emu.OperationStatus = 2;
        }

        /// <summary>将 EMU 控制命令回写到 ESS 物理模型。</summary>
        public static void ApplyEmuCommands(EnergyManagementData emu, EnergyStorageSystem ess)
        {
            if (emu.PcsList == null || emu.PcsList.Count == 0) return;

            var pcs1 = ess._pcs1;
            var pcs2 = ess._pcs2;

            // 功率设定命令
            if (Math.Abs(emu.PcsList[0].PCSActivePowerSetting   - pcs1.GetCurrentState().ActivePower)   > 0 ||
                Math.Abs(emu.PcsList[0].PCSReactivePowerSetting  - pcs1.GetCurrentState().ReactivePower) > 0)
                pcs1.SetPowerCommand(emu.PcsList[0].PCSActivePowerSetting, emu.PcsList[0].PCSReactivePowerSetting);

            if (Math.Abs(emu.PcsList[1].PCSActivePowerSetting   - pcs2.GetCurrentState().ActivePower)   > 0 ||
                Math.Abs(emu.PcsList[1].PCSReactivePowerSetting  - pcs2.GetCurrentState().ReactivePower) > 0)
                pcs2.SetPowerCommand(emu.PcsList[1].PCSActivePowerSetting, emu.PcsList[1].PCSReactivePowerSetting);

            // 调度模式同步（通过线程安全方法写入，避免直接操作 GetCurrentState() 引用）
            var state1 = pcs1.GetCurrentState();
            if (emu.PcsList[0].ActivePowerDispatchMode   != state1.ActiveDispathMode ||
                emu.PcsList[0].ReactivePowerDispatchMode != state1.ReactiveDispathMode)
                pcs1.SetDispatchMode(emu.PcsList[0].ActivePowerDispatchMode,
                                     emu.PcsList[0].ReactivePowerDispatchMode);

            var state2 = pcs2.GetCurrentState();
            if (emu.PcsList[1].ActivePowerDispatchMode   != state2.ActiveDispathMode ||
                emu.PcsList[1].ReactivePowerDispatchMode != state2.ReactiveDispathMode)
                pcs2.SetDispatchMode(emu.PcsList[1].ActivePowerDispatchMode,
                                     emu.PcsList[1].ReactivePowerDispatchMode);
        }
    }
}

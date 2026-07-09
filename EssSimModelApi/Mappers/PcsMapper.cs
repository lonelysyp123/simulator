using EssSimulator;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;

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
            dst.SimulatorMode = src.Mode;
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

            dst.IslandVoltageFeedback = (float)src.IslandVoltageEffectiveV;
            dst.DriveFault = src.FaultType == 3;
            dst.OperationStatus = PcsDisplayLabels.ToOperationStatusCode(src, dst.pcsOnOffSwitch);
            // BlackStartEnabled 为 EMS 下发命令，勿用仿真状态回写覆盖（否则 Modbus 写 5305 后会被 MapPcsState 清回 0）
        }

        /// <summary>故障跳闸撤回启停后，将 DTO 启停位同步为 0，供 Modbus Hold 反馈写线圈。</summary>
        public static void SyncRunCommandFeedback(PcsDevice pcsSim, PcsData pcsData)
        {
            if (!pcsSim.IsExternalRunCommand && pcsData.pcsOnOffSwitch)
                pcsData.pcsOnOffSwitch = false;
        }

        /// <summary>更新 EMU 汇总数据（运行状态、SOC 等）。</summary>
        public static void MapEmuState(EnergyManagementData emu, IReadOnlyList<BatteryRackSimulator> batteryRacks)
        {
            emu.Emu.MaxChargePower    = 1250.0f;
            emu.Emu.MaxDischargePower = 1250.0f;

            if (batteryRacks.Count > 0 && batteryRacks[0].GetRackState() != null)
                emu.Emu.AverageBatterySoc = (float)batteryRacks[0].GetRackState().MinClusterSOC * 100;

            // 运行状态（1停机 2待机 4充电 5放电 6未知；正放负充）
            bool anyRunning = false, anyDischarge = false, anyCharge = false;
            foreach (var pcs in emu.PcsList)
            {
                if (pcs.SimulatorMode == OperationMode.Off)
                    continue;
                anyRunning = true;
                if (pcs.ActivePower > PcsDisplayLabels.ActivePowerThresholdKw)  anyDischarge = true;
                if (pcs.ActivePower < -PcsDisplayLabels.ActivePowerThresholdKw) anyCharge   = true;
            }
            emu.Emu.OperationStatus = anyDischarge ? 5 : (anyCharge ? 4 : (anyRunning ? 2 : 1));
        }

        /// <summary>
        /// 启动完成后置位启停命令并立即 ApplyEmuCommands。
        /// 返回 false 表示 emu/ess 尚未就绪，调用方可在下一周期重试。
        /// </summary>
        public static bool TryApplyStartupPcsStartStop(EnergyStorageSystem ess, int unitCount)
        {
            bool allReady = true;
            for (int u = 0; u < unitCount; u++)
            {
                var emu = SimulatorHost.Instance.Get<EnergyManagementData>($"emu{u + 1}");
                if (emu == null)
                {
                    allReady = false;
                    continue;
                }

                int baseIdx = u * 2;
                if (baseIdx + 1 >= ess._pcsList.Count)
                {
                    allReady = false;
                    continue;
                }

                emu.PcsList[0].pcsOnOffSwitch = true;
                emu.PcsList[1].pcsOnOffSwitch = true;
                ApplyEmuCommands(emu, ess, baseIdx);
            }

            return allReady;
        }

        private static void ClearBlackStartCommand(int simIdx, PcsData pcsData)
        {
            if (!pcsData.BlackStartEnabled)
                return;

            pcsData.BlackStartEnabled = false;
        }

        /// <summary>将 EMU 控制命令回写到 ESS 物理模型。</summary>
        public static void ApplyEmuCommands(EnergyManagementData emu, EnergyStorageSystem ess, int pcsBaseIndex = 0)
        {
            if (emu.PcsList == null || emu.PcsList.Count == 0) return;

            for (int i = 0; i < emu.PcsList.Count; i++)
            {
                var pcsData = emu.PcsList[i];
                int simIdx = pcsBaseIndex + i;
                if (simIdx < 0 || simIdx >= ess._pcsList.Count) break;
                var pcsSim  = ess._pcsList[simIdx];

                bool cmdOn = pcsData.pcsOnOffSwitch;

                if (!cmdOn)
                {
                    if (pcsSim.IsExternalRunCommand)
                        pcsSim.SyncExternalRunCommand(false);
                    ess.PushPcsChannelToNetwork(simIdx);
                    continue;
                }

                bool mainBreakerClosed = ess.IsMainBreakerClosed;
                int unitIdx = simIdx / 2;
                bool unitBreakerClosed = ess.IsUnitBreakerClosed(unitIdx);
                bool breakersOpen = !mainBreakerClosed || !unitBreakerClosed;

                // 无网：主断分，或该单元高压分。允许「主断分+单元合+黑启动」；禁止「主断合+单元合+黑启动」
                if (breakersOpen && !pcsData.BlackStartEnabled)
                {
                    pcsSim.SyncExternalRunCommand(false);
                    pcsSim.ApplyIslandVoltageCommand(0);
                    pcsSim.ApplyBlackStartEnabled(false);
                    ClearBlackStartCommand(simIdx, pcsData);
                    pcsSim.TransitionToMode(OperationMode.Off, "主断/单元高压分闸且无黑启动");
                    ess.PushPcsChannelToNetwork(simIdx);
                    continue;
                }

                pcsSim.SyncExternalRunCommand(true);

                ApplyOperationalMode(pcsData, pcsSim, ess, simIdx);

                if (breakersOpen || pcsData.BlackStartEnabled)
                {
                    ess.PushPcsChannelToNetwork(simIdx);
                    continue;
                }

                pcsSim.SetPowerCommand(pcsData.PCSActivePowerSetting, pcsData.PCSReactivePowerSetting);
                ess.PushPcsChannelToNetwork(simIdx);
            }
        }

        private static void ApplyOperationalMode(
            PcsData pcsData,
            PcsDevice pcsSim,
            EnergyStorageSystem ess,
            int simIdx)
        {
            if (pcsSim.HasLatchedFaultTrip)
            {
                pcsSim.WithdrawExternalRunCommand();
                pcsSim.TransitionToMode(OperationMode.Off, "故障已锁存，等待启停写 1 复归");
                return;
            }

            double maxIslandV = pcsSim._config.AcVoltageNominal;

            if (!ess.TrySetPcsBlackStart(simIdx, pcsData.BlackStartEnabled))
            {
                pcsSim.ApplyBlackStartEnabled(false);
                BlackStartSafety.ReportViolation("开启黑启动", simIdx + 1);
                ClearBlackStartCommand(simIdx, pcsData);
                pcsSim.TransitionToMode(OperationMode.Off, "黑启动安全联锁拒绝开启");
                return;
            }

            int unit = simIdx / 2;
            double busV = ess.GetUnitAcBusVoltage(unit);
            pcsSim.RefreshBlackStartBusContext(busV);

            if (!pcsSim.IsBlackStartSynchronized)
            {
                ushort islandSet = (ushort)Math.Min(pcsData.IslandVoltageSetting, maxIslandV);
                if (islandSet != pcsData.IslandVoltageSetting)
                    pcsData.IslandVoltageSetting = islandSet;
                pcsSim.ApplyIslandVoltageCommand(islandSet);
            }

            if (!pcsSim.IsGridElectricallyAvailable)
            {
                if (pcsData.BlackStartEnabled || pcsData.IslandVoltageSetting > 0)
                    pcsSim.TransitionToMode(OperationMode.Normal, "网侧无电，黑启动/孤岛建压运行");
                else
                    pcsSim.TransitionToMode(OperationMode.Standby, "网侧无电且无黑启动/孤岛电压设定");
            }
            else
                pcsSim.TransitionToMode(OperationMode.Normal, "网侧有电，跟网运行");
        }
    }
}

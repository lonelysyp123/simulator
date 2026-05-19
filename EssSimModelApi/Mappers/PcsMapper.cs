using EssSimulator;
using EssSimulator.Core;
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

            dst.IslandVoltagePercentFeedback = (float)src.IslandVoltagePercentEffective;
            dst.BlackStartEnabled = src.BlackStartEnabled;
        }

        /// <summary>更新 EMU 汇总数据（运行状态、SOC 等）。</summary>
        public static void MapEmuState(EnergyManagementData emu, IReadOnlyList<BatteryRackSimulator> batteryRacks)
        {
            emu.Emu.MaxChargePower    = 1250.0f;
            emu.Emu.MaxDischargePower = 1250.0f;

            if (batteryRacks.Count > 0 && batteryRacks[0].GetRackState() != null)
                emu.Emu.AverageBatterySoc = (float)batteryRacks[0].GetRackState().MinClusterSOC * 100;

            // 运行状态判断（正放负充：ActivePower>10kW为放电，<-10kW为充电）
            bool anyDischarge = false, anyCharge = false;
            foreach (var pcs in emu.PcsList)
            {
                if (pcs.SimulatorMode == OperationMode.Off)
                    continue;
                if (pcs.ActivePower > PcsDisplayLabels.ActivePowerThresholdKw)  anyDischarge = true;
                if (pcs.ActivePower < -PcsDisplayLabels.ActivePowerThresholdKw) anyCharge   = true;
            }
            emu.Emu.OperationStatus = anyDischarge ? 4 : (anyCharge ? 3 : 2);
        }

        /// <summary>
        /// 联锁/外部停机后，将启停命令位清 0 并回写 Modbus，便于外部再次写 1 触发变位。
        /// 不在「外部刚写 1 等待启动」时调用，否则会立刻把 Modbus 写回 0，表现为 mbpoll 成功但界面无反应。
        /// </summary>
        private static void ClearPcsStartStopCommand(int simIdx, PcsData pcsData)
        {
            if (!pcsData.pcsOnOffSwitch)
                return;

            pcsData.pcsOnOffSwitch = false;

            int unitIndex0 = simIdx / 2;
            int slotInUnit = simIdx % 2;
            string paramName = slotInUnit == 0 ? "pcs1_startstop" : "pcs2_startstop";
            var modbus = SimulatorHost.Instance.Get<ModbusSimServer>($"simEmu{unitIndex0 + 1}");
            modbus?.PublishControlToSlave(paramName, false);
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
                pcsSim.SyncExternalRunCommand(cmdOn);

                bool mainBreakerClosed = ess._breaker.IsClosed;
                int unitIndex = simIdx / 2;
                bool unitBreakerClosed = unitIndex < ess._unitBreakers.Count &&
                                         ess._unitBreakers[unitIndex].IsClosed;

                if (!cmdOn)
                    continue;

                bool breakersOpen = !mainBreakerClosed || !unitBreakerClosed;

                // 黑启动：主断/单元高压分闸时仍允许离网建压（合闸+黑启动由 BlackStartSafety 禁止）
                if (breakersOpen && !pcsData.BlackStartEnabled)
                {
                    pcsSim.ApplyIslandVoltagePercentCommand(0);
                    pcsSim.ApplyBlackStartEnabled(false);
                    pcsData.BlackStartEnabled = false;
                    pcsSim.TransitionToMode(OperationMode.Off);
                    ClearPcsStartStopCommand(simIdx, pcsData);
                    continue;
                }

                // 外部启停为 1 时持续尝试进入运行（并网或黑启动离网）
                ApplyOperationalMode(pcsData, pcsSim, ess, simIdx);

                if (pcsData.pcsOnOffSwitch && !pcsData.BlackStartEnabled && !breakersOpen)
                {
                    if (Math.Abs(pcsData.PCSActivePowerSetting  - pcsSim.GetCurrentState().ActivePower)  > 0 ||
                        Math.Abs(pcsData.PCSReactivePowerSetting - pcsSim.GetCurrentState().ReactivePower) > 0)
                        pcsSim.SetPowerCommand(pcsData.PCSActivePowerSetting, pcsData.PCSReactivePowerSetting);
                }
            }
        }

        private static void ApplyOperationalMode(
            PcsData pcsData,
            PCSSimulator pcsSim,
            EnergyStorageSystem ess,
            int simIdx)
        {
            pcsSim.ApplyIslandVoltagePercentCommand(pcsData.IslandVoltagePercentSetting);

            if (pcsData.BlackStartEnabled &&
                !BlackStartSafety.TryEnableBlackStart(ess, simIdx, true))
            {
                pcsData.BlackStartEnabled = false;
                pcsSim.ApplyBlackStartEnabled(false);
                pcsSim.TransitionToMode(OperationMode.Off);
                return;
            }

            pcsSim.ApplyBlackStartEnabled(pcsData.BlackStartEnabled);

            if (!pcsSim.IsGridElectricallyAvailable)
            {
                if (pcsData.BlackStartEnabled || pcsData.IslandVoltagePercentSetting > 0)
                    pcsSim.TransitionToMode(OperationMode.Normal);
                else
                    pcsSim.TransitionToMode(OperationMode.Standby);
            }
            else
                pcsSim.TransitionToMode(OperationMode.Normal);
        }
    }
}

using EssSimulator;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>
    /// 将 ESS 物理模型数据映射到 PCS/EMU 接口数据对象，以及将 EMU 控制命令回写到模型。
    /// </summary>
    public static class PcsMapper
    {
        /// <summary>BMS 黑启动状态：已进入（建压前置条件）。</summary>
        private const ushort BmsBlackStartStatusActive = 3;

        /// <summary>BMS 黑启动进入成功标志。</summary>
        private const ushort BmsBlackStartEnterSuccess = 1;

        /// <summary>
        /// 校验对应 BMS 是否已进入黑启动模式（状态=3 且 进入成功=1）。
        /// PCS 黑启动前 BMS 必须先完成自身校验并确认就绪。
        /// </summary>
        private static bool IsBmsBlackStartReady(int pcsSimIndex)
        {
            var bmsData = SimulatorHost.Instance
                .Get<BatteryManagementSystemData>($"bms{pcsSimIndex + 1}");
            if (bmsData?.BatteryStacks == null || bmsData.BatteryStacks.Count == 0)
                return false;

            var stack = bmsData.BatteryStacks[0];
            return stack.BlackStartStatus == BmsBlackStartStatusActive
                && stack.BlackStartEnterSuccess == BmsBlackStartEnterSuccess;
        }

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
            // 可用容量（%）：从所属电池堆 SOC 推导（无 BMS 关联时回退 100）
            dst.AvailableCapacity  = bms != null ? (float)Math.Clamp(bms.GetRackSOC() * 100.0, 0, 100) : 100f;

            double denom = Math.Sqrt(Math.Pow(src.ActivePower, 2) + Math.Pow(src.ReactivePower, 2));
            dst.ApparentPower = (float)denom;
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

        /// <summary>更新 EMU 汇总数据（运行状态、SOC、单元总有功/无功与 PCS 统计）。</summary>
        public static void MapEmuState(EnergyManagementData emu, IReadOnlyList<BatteryRackSimulator> batteryRacks)
        {
            if (batteryRacks.Count > 0 && batteryRacks[0].GetRackState() != null)
                emu.Emu.AverageBatterySoc = (float)batteryRacks[0].GetRackState().MinClusterSOC * 100;

            // 单元总有功/无功 = PcsList 各路之和；同步统计台数/告警/故障/禁充禁放
            float sumP = 0f, sumQ = 0f;
            float sumChargeLimit = 0f, sumDischargeLimit = 0f, sumRated = 0f;
            int online = 0, gridConnected = 0, alarmed = 0, faulted = 0;
            int chargeProhibited = 0, dischargeProhibited = 0;
            bool anyDischarge = false, anyCharge = false;
            bool anyStarted = false;
            bool allShutdown = true, allAlarmed = true, allFaulted = true;
            bool allChargeProhibited = true, allDischargeProhibited = true;

            foreach (var pcs in emu.PcsList)
            {
                sumP += pcs.ActivePower;
                sumQ += pcs.ReactivePower;
                sumChargeLimit += pcs.ChargePowerLimit;
                sumDischargeLimit += pcs.DischargePowerLimit;
                sumRated += pcs.PCSRatePower;

                bool off = pcs.SimulatorMode == OperationMode.Off;
                bool fault = pcs.OperationStatus == 6;
                bool alarm = !fault && HasAnyAlarm(pcs);
                if (!off) { online++; allShutdown = false; }
                if (pcs.SimulatorMode == OperationMode.Normal) { gridConnected++; anyStarted = true; }
                if (fault) { faulted++; }
                else { allFaulted = false; }
                if (alarm) alarmed++;
                if (!alarm) allAlarmed = false;
                if (pcs.ChargeProhibited) chargeProhibited++; else allChargeProhibited = false;
                if (pcs.DischargeProhibited) dischargeProhibited++; else allDischargeProhibited = false;

                if (off) continue;
                if (pcs.ActivePower > PcsDisplayLabels.ActivePowerThresholdKw)  anyDischarge = true;
                if (pcs.ActivePower < -PcsDisplayLabels.ActivePowerThresholdKw) anyCharge   = true;
            }

            int total = emu.PcsList.Count;
            emu.Emu.OutputActivePower = sumP;
            emu.Emu.OutputReactivePower = sumQ;

            // 功率能力：优先取各 PCS 充/放电限值之和，未配置时回退额定容量之和
            emu.Emu.MaxChargePower    = sumChargeLimit > 0 ? sumChargeLimit : sumRated;
            emu.Emu.MaxDischargePower = sumDischargeLimit > 0 ? sumDischargeLimit : sumRated;
            emu.Emu.MaxInductiveReactivePower = sumRated;
            emu.Emu.MaxCapacitiveReactivePower = sumRated;

            emu.Emu.TotalPcsCount = total;
            emu.Emu.OnlinePcsCount = online;
            emu.Emu.GridConnectedPcsCount = gridConnected;
            emu.Emu.AlarmPcsCount = alarmed;
            emu.Emu.FaultPcsCount = faulted;
            emu.Emu.ChargeProhibitedPcsCount = chargeProhibited;
            emu.Emu.DischargeProhibitedPcsCount = dischargeProhibited;

            emu.Emu.AllPcsShutdown = total > 0 && allShutdown;
            emu.Emu.AnyPcsStarted = anyStarted;
            emu.Emu.AllPcsAlarmed = total > 0 && allAlarmed;
            emu.Emu.AnyPcsAlarmed = alarmed > 0;
            emu.Emu.AllPcsFaulted = total > 0 && allFaulted;
            emu.Emu.AnyPcsFaulted = faulted > 0;
            emu.Emu.AllPcsChargeProhibited = total > 0 && allChargeProhibited;
            emu.Emu.AnyPcsChargeProhibited = chargeProhibited > 0;
            emu.Emu.AllPcsDischargeProhibited = total > 0 && allDischargeProhibited;
            emu.Emu.AnyPcsDischargeProhibited = dischargeProhibited > 0;

            // 运行状态（1停机 2待机 4充电 5放电 6未知；正放负充）
            emu.Emu.OperationStatus = anyDischarge ? 5 : (anyCharge ? 4 : (online > 0 ? 2 : 1));
        }

        /// <summary>PCS 是否携带任一告警字（告警 1~7 任一非 0）。</summary>
        private static bool HasAnyAlarm(PcsData pcs) =>
            pcs.AlarmSummary1 != 0 || pcs.AlarmSummary2 != 0 || pcs.AlarmSummary3 != 0 ||
            pcs.AlarmSummary4 != 0 || pcs.AlarmSummary5 != 0 || pcs.AlarmSummary6 != 0 ||
            pcs.AlarmSummary7 != 0;

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

                int baseIdx = ess.PcsBaseIndexOfUnit(u);
                int pcsCount = emu.PcsList.Count;
                if (pcsCount == 0 || baseIdx + pcsCount > ess._pcsList.Count)
                {
                    allReady = false;
                    continue;
                }

                foreach (var pcsData in emu.PcsList)
                    pcsData.pcsOnOffSwitch = true;
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
                int unitIdx = ess.UnitIndexOfPcs(simIdx);
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

            // BMS 黑启动状态校验：PCS 建压前 BMS 必须先进入黑启动模式
            if (pcsData.BlackStartEnabled && !IsBmsBlackStartReady(simIdx))
            {
                pcsSim.ApplyBlackStartEnabled(false);
                ClearBlackStartCommand(simIdx, pcsData);
                pcsSim.TransitionToMode(OperationMode.Off, "BMS 未进入黑启动模式，拒绝 PCS 黑启动");
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

            int unit = ess.UnitIndexOfPcs(simIdx);
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

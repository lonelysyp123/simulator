using EssSimulator.EssSimModelApi.BatteryManagementSystem;

namespace EssSimulator.EssDeviceSimModel.Devices
{
    /// <summary>
    /// BMS 保护逻辑：簇级阈值评估 + Rack 级告警汇总回写物理状态。
    /// </summary>
    public static class BmsRackProtection
    {
        public static void EvaluateAllClusters(BatteryRackSimulator rackSim, BatteryManagementSystemData bmsData)
        {
            var clusterStates = rackSim.GetRackState().ClusterStates;
            var clusterConfig = rackSim.GetRackConfig().ClusterConfig;
            int packSerial = rackSim._clusters[0]._packs[0].GetPackConfiguration().SeriesCount;
            var stack = bmsData.BatteryStacks[0];

            for (int i = 0; i < clusterStates.Count; i++)
            {
                var cs = clusterStates[i];
                var clu = stack.Cluseter[i];
                EvaluateCluster(
                    cs,
                    clusterConfig.PackCount,
                    packSerial,
                    clu.Thresholds,
                    clu.Alarms,
                    clu.Measurements.Insulation ?? 0f);
            }
        }

        public static void EvaluateCluster(
            ClusterState clusterState,
            int packCount,
            int cellsPerPack,
            ClusterThresholds thresholds,
            ClusterAlarms alarms,
            float insulationValue,
            float busbarTempC = 26.0f)
        {
            var minVoltList = Enumerable.Range(0, packCount)
                .Select(j => (float)clusterState.PackStates[j].MinCellVoltage).ToList();
            var maxVoltList = Enumerable.Range(0, packCount)
                .Select(j => (float)clusterState.PackStates[j].MaxCellVoltage).ToList();
            var minTempList = Enumerable.Range(0, packCount)
                .Select(j => (float)clusterState.PackStates[j].MinCellTemp).ToList();
            var maxTempList = Enumerable.Range(0, packCount)
                .Select(j => (float)clusterState.PackStates[j].MaxCellTemp).ToList();

            var thr = thresholds;
            var alm = alarms;
            bool? l1, l2, l3;

            (l1, l2, l3) = (alm.UndervoltageProtection, alm.UndervoltageAlarm, alm.UndervoltageFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.UndervoltageThreshold1!.Value, thr.UndervoltageThreshold2!.Value, thr.UndervoltageThreshold3!.Value, thr.UndervoltageRecovery1!.Value, thr.UndervoltageRecovery2!.Value, thr.UndervoltageRecovery3!.Value, (float)clusterState.TotalVoltage);
            (alm.UndervoltageProtection, alm.UndervoltageAlarm, alm.UndervoltageFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.OvervoltageProtection, alm.OvervoltageAlarm, alm.OvervoltageFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.OvervoltageThreshold1!.Value, thr.OvervoltageThreshold2!.Value, thr.OvervoltageThreshold3!.Value, thr.OvervoltageRecovery1!.Value, thr.OvervoltageRecovery2!.Value, thr.OvervoltageRecovery3!.Value, (float)clusterState.TotalVoltage);
            (alm.OvervoltageProtection, alm.OvervoltageAlarm, alm.OvervoltageFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.ChargeOvercurrentProtection, alm.ChargeOvercurrentAlarm, alm.ChargeOvercurrentFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.ChargeOvercurrentThreshold1!.Value, thr.ChargeOvercurrentThreshold2!.Value, thr.ChargeOvercurrentThreshold3!.Value, thr.ChargeOvercurrentRecovery1!.Value, thr.ChargeOvercurrentRecovery2!.Value, thr.ChargeOvercurrentRecovery3!.Value, (float)(-clusterState.TotalCurrent));
            (alm.ChargeOvercurrentProtection, alm.ChargeOvercurrentAlarm, alm.ChargeOvercurrentFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.DischargeOvercurrentProtection, alm.DischargeOvercurrentAlarm, alm.DischargeOvercurrentFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.DischargeOvercurrentThreshold1!.Value, thr.DischargeOvercurrentThreshold2!.Value, thr.DischargeOvercurrentThreshold3!.Value, thr.DischargeOvercurrentRecovery1!.Value, thr.DischargeOvercurrentRecovery2!.Value, thr.DischargeOvercurrentRecovery3!.Value, (float)clusterState.TotalCurrent);
            (alm.DischargeOvercurrentProtection, alm.DischargeOvercurrentAlarm, alm.DischargeOvercurrentFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellUnderVoltageProtection, alm.CellUnderVoltageAlarm, alm.CellUnderVoltageFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.CellUndervoltageThreshold1!.Value, thr.CellUndervoltageThreshold2!.Value, thr.CellUndervoltageThreshold3!.Value, thr.CellUndervoltageRecovery1!.Value, thr.CellUndervoltageRecovery2!.Value, thr.CellUndervoltageRecovery3!.Value, minVoltList.Min());
            (alm.CellUnderVoltageProtection, alm.CellUnderVoltageAlarm, alm.CellUnderVoltageFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellOverVoltageProtection, alm.CellOverVoltageAlarm, alm.CellOverVoltageFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.CellOvervoltageThreshold1!.Value, thr.CellOvervoltageThreshold2!.Value, thr.CellOvervoltageThreshold3!.Value, thr.CellOvervoltageRecovery1!.Value, thr.CellOvervoltageRecovery2!.Value, thr.CellOvervoltageRecovery3!.Value, maxVoltList.Max());
            (alm.CellOverVoltageProtection, alm.CellOverVoltageAlarm, alm.CellOverVoltageFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.VoltageDifferenceProtection, alm.VoltageDifferenceAlarm, alm.VoltageDifferenceFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.CellVoltageDifferenceThreshold1!.Value, thr.CellVoltageDifferenceThreshold2!.Value, thr.CellVoltageDifferenceThreshold3!.Value, thr.CellVoltageDifferenceRecovery1!.Value, thr.CellVoltageDifferenceRecovery2!.Value, thr.CellVoltageDifferenceRecovery3!.Value, maxVoltList.Max() - minVoltList.Min());
            (alm.VoltageDifferenceProtection, alm.VoltageDifferenceAlarm, alm.VoltageDifferenceFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.TempDifferenceProtection, alm.TempDifferenceAlarm, alm.TempDifferenceFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.CellTempDifferenceThreshold1!.Value, thr.CellTempDifferenceThreshold2!.Value, thr.CellTempDifferenceThreshold3!.Value, thr.CellTempDifferenceRecovery1!.Value, thr.CellTempDifferenceRecovery2!.Value, thr.CellTempDifferenceRecovery3!.Value, maxTempList.Max() - minTempList.Min());
            (alm.TempDifferenceProtection, alm.TempDifferenceAlarm, alm.TempDifferenceFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.LowSOCProtection, alm.LowSOCAlarm, alm.LowSOCFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.LowSOCTreshold1!.Value, thr.LowSOCTreshold2!.Value, thr.LowSOCTreshold3!.Value, thr.LowSOCRecovery1!.Value, thr.LowSOCRecovery2!.Value, thr.LowSOCRecovery3!.Value, (float)clusterState.MinPackSOC);
            (alm.LowSOCProtection, alm.LowSOCAlarm, alm.LowSOCFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellChargeHighTempProtection, alm.CellChargeHighTempAlarm, alm.CellChargeHighTempFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.ChargeHighTempThreshold1!.Value, thr.ChargeHighTempThreshold2!.Value, thr.ChargeHighTempThreshold3!.Value, thr.ChargeHighTempRecovery1!.Value, thr.ChargeHighTempRecovery2!.Value, thr.ChargeHighTempRecovery3!.Value, maxTempList.Max());
            (alm.CellChargeHighTempProtection, alm.CellChargeHighTempAlarm, alm.CellChargeHighTempFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellChargeLowTempProtection, alm.CellChargeLowTempAlarm, alm.CellChargeLowTempFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.ChargeLowTempThreshold1!.Value, thr.ChargeLowTempThreshold2!.Value, thr.ChargeLowTempThreshold3!.Value, thr.ChargeLowTempRecovery1!.Value, thr.ChargeLowTempRecovery2!.Value, thr.ChargeLowTempRecovery3!.Value, minTempList.Min());
            (alm.CellChargeLowTempProtection, alm.CellChargeLowTempAlarm, alm.CellChargeLowTempFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.InsulationProtection, alm.InsulationAlarm, alm.InsulationFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.InsulationThreshold1!.Value, thr.InsulationThreshold2!.Value, thr.InsulationThreshold3!.Value, thr.InsulationRecovery1!.Value, thr.InsulationRecovery2!.Value, thr.InsulationRecovery3!.Value, insulationValue);
            (alm.InsulationProtection, alm.InsulationAlarm, alm.InsulationFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellDischargeHighTempProtection, alm.CellDischargeHighTempAlarm, alm.CellDischargeHighTempFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.DischargeHighTempThreshold1!.Value, thr.DischargeHighTempThreshold2!.Value, thr.DischargeHighTempThreshold3!.Value, thr.DischargeHighTempRecovery1!.Value, thr.DischargeHighTempRecovery2!.Value, thr.DischargeHighTempRecovery3!.Value, maxTempList.Max());
            (alm.CellDischargeHighTempProtection, alm.CellDischargeHighTempAlarm, alm.CellDischargeHighTempFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.CellDischargeLowTempProtection, alm.CellDischargeLowTempAlarm, alm.CellDischargeLowTempFault);
            UpdateUnder(ref l1, ref l2, ref l3, thr.DischargeLowTempThreshold1!.Value, thr.DischargeLowTempThreshold2!.Value, thr.DischargeLowTempThreshold3!.Value, thr.DischargeLowTempRecovery1!.Value, thr.DischargeLowTempRecovery2!.Value, thr.DischargeLowTempRecovery3!.Value, minTempList.Min());
            (alm.CellDischargeLowTempProtection, alm.CellDischargeLowTempAlarm, alm.CellDischargeLowTempFault) = (l1, l2, l3);

            (l1, l2, l3) = (alm.BatteryBoxBusbarHighTempProtection, alm.BatteryBoxBusbarHighTempAlarm, alm.BatteryBoxBusbarHighTempFault);
            UpdateOver(ref l1, ref l2, ref l3, thr.HVBHighTempThreshold1!.Value, thr.HVBHighTempThreshold2!.Value, thr.HVBHighTempThreshold3!.Value, thr.HVBHighTempRecovery1!.Value, thr.HVBHighTempRecovery2!.Value, thr.HVBHighTempRecovery3!.Value, busbarTempC);
            (alm.BatteryBoxBusbarHighTempProtection, alm.BatteryBoxBusbarHighTempAlarm, alm.BatteryBoxBusbarHighTempFault) = (l1, l2, l3);
        }

        /// <summary>
        /// Rack 级汇总：读取各簇告警汇总与 stack 级 SOC/SOH 规则，写回 <see cref="RackState"/> 故障态。
        /// </summary>
        public static void ApplyRackFaultSummary(
            BatteryManagementSystemData bmsData,
            RackState rack,
            int stackIndex = 0)
        {
            var stack = bmsData.BatteryStacks[stackIndex];

            if (stack.BMSFaultSummary != 0)
            {
                rack.IsFault = (ushort)((stack.IsChargeFault, stack.IsDischargeFault) switch
                {
                    (true, true) => 3,
                    (true, false) => 1,
                    (false, true) => 2,
                    (false, false) => 3
                });
            }
            else
            {
                rack.IsFault = 0;
            }

            if (stack.SOC >= 0.95f)
            {
                rack.IsFault = (ushort)(rack.IsFault == 0 ? 1
                                      : rack.IsFault == 2 ? 3
                                      : rack.IsFault);
            }

            if (stack.SOC <= 0.05f)
            {
                rack.IsFault = (ushort)(rack.IsFault == 0 ? 2
                                      : rack.IsFault == 1 ? 3
                                      : rack.IsFault);
            }

            if (stack.SOH <= 0.05f)
                rack.IsFault = 3;

            rack.IsAlarm = stack.BMSAlarmSummary != 0;
            rack.IsProtection = stack.BMSProtectionSummary != 0;
        }

        public static void UpdateUnder(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val)
        {
            if (l3 == true)
            { if (val > r3) { l3 = false; l2 = true; } }
            else if (l2 == true)
            { if (val <= t3) { l3 = true; l2 = false; } else if (val > r2) { l2 = false; l1 = true; } }
            else if (l1 == true)
            { if (val <= t2) { l2 = true; l1 = false; } else if (val > r1) { l1 = false; } }
            else
            { if (val <= t1) { l1 = true; } }
        }

        public static void UpdateOver(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val)
        {
            if (l3 == true)
            { if (val < r3) { l3 = false; l2 = true; } }
            else if (l2 == true)
            { if (val >= t3) { l3 = true; l2 = false; } else if (val < r2) { l2 = false; l1 = true; } }
            else if (l1 == true)
            { if (val >= t2) { l2 = true; l1 = false; } else if (val < r1) { l1 = false; } }
            else
            { if (val >= t1) { l1 = true; } }
        }
    }
}

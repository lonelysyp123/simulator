using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using static EssSimulator.EssDeviceSimModel.BatteryRackSimulator;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>
    /// 将 ESS 物理模型数据映射到 BMS 接口数据对象。
    /// 所有方法均为纯函数（无副作用），由 BmsDataService 调用。
    /// </summary>
    public static class BmsMapper
    {
        // ── 运行状态 ──────────────────────────────────────────────────

        public static int GetOperationStatus(float current) =>
            current > 0 ? 1 : current < 0 ? 2 : 0;

        // ── Rack → Stack 数据映射 ─────────────────────────────────────

        public static void MapRackToStack(RackState rack, BatteryManagementSystemData bmsData)
        {
            if (rack == null || bmsData == null) return;

            var stack = bmsData.BatteryStacks[0];
            stack.TotalVoltage = (float)rack.TotalVoltage;
            stack.Current      = (float)rack.TotalCurrent;
            stack.Power        = (float)rack.TotalVoltage * (float)rack.TotalCurrent / 1000.0f;
            stack.SOC          = (float)rack.MinClusterSOC;
            stack.SOH          = (float)rack.StateOfHealth;
            stack.Cycles       = 98;

            FindExtreme(rack, 1, out var val, out var cid, out var pid, out var eid);
            stack.MaxCellVoltage = val; stack.MaxCellVoltageClusterId = cid;
            stack.MaxCellVoltagePackId = pid; stack.MaxCellVoltageCellId = eid;

            FindExtreme(rack, 2, out val, out cid, out pid, out eid);
            stack.MinCellVoltage = val; stack.MinCellVoltageClusterId = cid;
            stack.MinCellVoltagePackId = pid; stack.MinCellVoltageCellId = eid;

            FindExtreme(rack, 3, out val, out cid, out pid, out eid);
            stack.MaxCellTemp = val; stack.MaxCellTempClusterId = cid;
            stack.MaxCellTempPackId = pid; stack.MaxCellTempCellId = eid;

            FindExtreme(rack, 4, out val, out cid, out pid, out eid);
            stack.MinCellTemp = val; stack.MinCellTempClusterId = cid;
            stack.MinCellTempPackId = pid; stack.MinCellTempCellId = eid;

            stack.AvgCellVoltage  = (float)rack.TotalVoltage / 416;
            stack.CellVoltageDiff = (float)rack.VoltageDifference;
            stack.AvgCellTemp     = (float)rack.AvgClusterTemp;
            stack.CellTempDiff    = (float)rack.MaxClusterTemp - (float)rack.MinClusterTemp;
            stack.MaxCellSOC      = (float)rack.MaxClusterSOC;
            stack.MinCellSOC      = (float)rack.MinClusterSOC;
            stack.CumulativeChargeEnergy    = (float)rack.TotalChargeEnergy;
            stack.CumulativeDischargeEnergy = (float)rack.TotalDischargeEnergy;
        }

        /// <summary>
        /// 将 BMS 故障标志反向写回物理模型 RackState.IsFault/IsAlarm/IsProtection。
        /// </summary>
        /// <param name="bmsData">BMS 数据对象</param>
        /// <param name="rack">目标物理 Rack 状态</param>
        /// <param name="stackIndex">读取的 Stack 索引，默认为 0</param>
        public static void SyncFaultToRack(BatteryManagementSystemData bmsData, RackState rack, int stackIndex = 0)
        {
            var stack = bmsData.BatteryStacks[stackIndex];

            // 根据 BMS 上报的充/放电故障标志编码 IsFault
            if (stack.BMSFaultSummary != 0)
            {
                rack.IsFault = (ushort)((stack.IsChargeFault, stack.IsDischargeFault) switch
                {
                    (true,  true)  => 3,  // 充放电均故障
                    (true,  false) => 1,  // 仅充电故障
                    (false, true)  => 2,  // 仅放电故障
                    (false, false) => 3   // BMSFaultSummary 有故障但标志未细分 → 按其他故障处理
                });
            }
            else
            {
                rack.IsFault = 0;
            }

            // SOC 过高（≥95%）：满电，禁止充电 → 叠加充电故障(1)
            if (stack.SOC >= 0.95f)
            {
                rack.IsFault = (ushort)(rack.IsFault == 0 ? 1        // 无故障     → 充电故障
                                      : rack.IsFault == 2 ? 3        // 放电故障   → 升级为全故障
                                      : rack.IsFault);               // 已含充电故障，保留
            }

            // SOC 过低（≤5%）：电量耗尽，禁止放电 → 叠加放电故障(2)
            if (stack.SOC <= 0.05f)
            {
                rack.IsFault = (ushort)(rack.IsFault == 0 ? 2        // 无故障     → 放电故障
                                      : rack.IsFault == 1 ? 3        // 充电故障   → 升级为全故障
                                      : rack.IsFault);               // 已含放电故障，保留
            }

            // SOH 过低（≤5%）：健康度极低，充放电均应禁止 → 强制全故障(3)
            if (stack.SOH <= 0.05f)
            {
                rack.IsFault = 3;
            }

            rack.IsAlarm      = stack.BMSAlarmSummary      != 0;
            rack.IsProtection = stack.BMSProtectionSummary != 0;
        }

        // ── 簇级数据映射 ──────────────────────────────────────────────

        /// <summary>
        /// 将 BatteryRackSimulator 中每个簇的状态写入 bmsData 的簇测量值和告警状态。
        /// </summary>
        public static void MapClusters(BatteryRackSimulator rackSim, BatteryManagementSystemData bmsData)
        {
            var clusterStates = rackSim.GetRackState().ClusterStates;
            var clusterConfig = rackSim.GetRackConfig().ClusterConfig;
            int packSerial = rackSim._clusters[0]._packs[0].GetPackConfiguration().SeriesCount;

            for (int i = 0; i < clusterStates.Count; i++)
            {
                var cs  = clusterStates[i];
                var clu = bmsData.BatteryStacks[0].Cluseter[i];

                // 单体电压/温度字典
                var voltDict = clu.ClusterCellVoltages.CellVoltages;
                var tempDict = clu.ClusterCellTemperatures.CellTemperatures;
                for (int j = 0; j < clusterConfig.PackCount; j++)
                    for (int k = 0; k < packSerial; k++)
                    {
                        int key = j * packSerial + k;
                        voltDict[key] = (float)cs.PackStates[j].CellStates[k].Voltage;
                        tempDict[key] = (float)cs.PackStates[j].CellStates[k].Temperature;
                    }

                // 基础簇测量值
                var m = clu.Measurements;
                m.TotalVoltage    = (float)cs.TotalVoltage;
                m.Current         = (float)cs.TotalCurrent;
                m.SOC             = (float)cs.AvgPackSOC;
                m.Power           = (float)cs.TotalVoltage * (float)cs.TotalCurrent;
                m.SOH             = (float)cs.StateOfHealth;
                m.OperationStatus = (ushort)GetOperationStatus((float)cs.TotalCurrent);
                m.AvgCellVoltage  = (float)cs.TotalVoltage / 416;
                m.AvgCellTemp     = (float)cs.AvgPackTemp;
                m.MaxCellSOC      = (float)cs.MaxPackSOC;
                m.MinCellSOC      = (float)cs.MinPackSOC;
                m.CellVoltageSum  = voltDict.Values.Sum();

                var maxV = voltDict.Aggregate((a, b) => a.Value > b.Value ? a : b);
                var minV = voltDict.Aggregate((a, b) => a.Value < b.Value ? a : b);
                m.MaxCellVoltage  = maxV.Value; m.MaxCellVoltageId = maxV.Key;
                m.MinCellVoltage  = minV.Value; m.MinCellVoltageId = minV.Key;

                var maxT = tempDict.Aggregate((a, b) => a.Value > b.Value ? a : b);
                var minT = tempDict.Aggregate((a, b) => a.Value < b.Value ? a : b);
                m.MaxCellTemp     = maxT.Value; m.MaxCellTempId = maxT.Key;
                m.MinCellTemp     = minT.Value; m.MinCellTempId = minT.Key;

                // 告警状态机 — 属性不能作为 ref，使用局部变量
                var thr = clu.Thresholds;
                var alm = clu.Alarms;

                var minVoltList = Enumerable.Range(0, clusterConfig.PackCount)
                    .Select(j => (float)cs.PackStates[j].MinCellVoltage).ToList();
                var maxVoltList = Enumerable.Range(0, clusterConfig.PackCount)
                    .Select(j => (float)cs.PackStates[j].MaxCellVoltage).ToList();
                var minTempList = Enumerable.Range(0, clusterConfig.PackCount)
                    .Select(j => (float)cs.PackStates[j].MinCellTemp).ToList();
                var maxTempList = Enumerable.Range(0, clusterConfig.PackCount)
                    .Select(j => (float)cs.PackStates[j].MaxCellTemp).ToList();

                bool? l1, l2, l3;

                // 簇电压过低
                (l1, l2, l3) = (alm.UndervoltageProtection, alm.UndervoltageAlarm, alm.UndervoltageFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.UndervoltageThreshold1!.Value, thr.UndervoltageThreshold2!.Value, thr.UndervoltageThreshold3!.Value, thr.UndervoltageRecovery1!.Value, thr.UndervoltageRecovery2!.Value, thr.UndervoltageRecovery3!.Value, (float)cs.TotalVoltage);
                (alm.UndervoltageProtection, alm.UndervoltageAlarm, alm.UndervoltageFault) = (l1, l2, l3);

                // 簇电压过高
                (l1, l2, l3) = (alm.OvervoltageProtection, alm.OvervoltageAlarm, alm.OvervoltageFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.OvervoltageThreshold1!.Value, thr.OvervoltageThreshold2!.Value, thr.OvervoltageThreshold3!.Value, thr.OvervoltageRecovery1!.Value, thr.OvervoltageRecovery2!.Value, thr.OvervoltageRecovery3!.Value, (float)cs.TotalVoltage);
                (alm.OvervoltageProtection, alm.OvervoltageAlarm, alm.OvervoltageFault) = (l1, l2, l3);

                // 充电过流
                (l1, l2, l3) = (alm.ChargeOvercurrentProtection, alm.ChargeOvercurrentAlarm, alm.ChargeOvercurrentFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.ChargeOvercurrentThreshold1!.Value, thr.ChargeOvercurrentThreshold2!.Value, thr.ChargeOvercurrentThreshold3!.Value, thr.ChargeOvercurrentRecovery1!.Value, thr.ChargeOvercurrentRecovery2!.Value, thr.ChargeOvercurrentRecovery3!.Value, (float)(-cs.TotalCurrent));
                (alm.ChargeOvercurrentProtection, alm.ChargeOvercurrentAlarm, alm.ChargeOvercurrentFault) = (l1, l2, l3);

                // 放电过流
                (l1, l2, l3) = (alm.DischargeOvercurrentProtection, alm.DischargeOvercurrentAlarm, alm.DischargeOvercurrentFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.DischargeOvercurrentThreshold1!.Value, thr.DischargeOvercurrentThreshold2!.Value, thr.DischargeOvercurrentThreshold3!.Value, thr.DischargeOvercurrentRecovery1!.Value, thr.DischargeOvercurrentRecovery2!.Value, thr.DischargeOvercurrentRecovery3!.Value, (float)cs.TotalCurrent);
                (alm.DischargeOvercurrentProtection, alm.DischargeOvercurrentAlarm, alm.DischargeOvercurrentFault) = (l1, l2, l3);

                // 单体电压过低
                (l1, l2, l3) = (alm.CellUnderVoltageProtection, alm.CellUnderVoltageAlarm, alm.CellUnderVoltageFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.CellUndervoltageThreshold1!.Value, thr.CellUndervoltageThreshold2!.Value, thr.CellUndervoltageThreshold3!.Value, thr.CellUndervoltageRecovery1!.Value, thr.CellUndervoltageRecovery2!.Value, thr.CellUndervoltageRecovery3!.Value, minVoltList.Min());
                (alm.CellUnderVoltageProtection, alm.CellUnderVoltageAlarm, alm.CellUnderVoltageFault) = (l1, l2, l3);

                // 单体电压过高
                (l1, l2, l3) = (alm.CellOverVoltageProtection, alm.CellOverVoltageAlarm, alm.CellOverVoltageFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.CellOvervoltageThreshold1!.Value, thr.CellOvervoltageThreshold2!.Value, thr.CellOvervoltageThreshold3!.Value, thr.CellOvervoltageRecovery1!.Value, thr.CellOvervoltageRecovery2!.Value, thr.CellOvervoltageRecovery3!.Value, maxVoltList.Max());
                (alm.CellOverVoltageProtection, alm.CellOverVoltageAlarm, alm.CellOverVoltageFault) = (l1, l2, l3);

                // 单体压差过大
                (l1, l2, l3) = (alm.VoltageDifferenceProtection, alm.VoltageDifferenceAlarm, alm.VoltageDifferenceFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.CellVoltageDifferenceThreshold1!.Value, thr.CellVoltageDifferenceThreshold2!.Value, thr.CellVoltageDifferenceThreshold3!.Value, thr.CellVoltageDifferenceRecovery1!.Value, thr.CellVoltageDifferenceRecovery2!.Value, thr.CellVoltageDifferenceRecovery3!.Value, maxVoltList.Max() - minVoltList.Min());
                (alm.VoltageDifferenceProtection, alm.VoltageDifferenceAlarm, alm.VoltageDifferenceFault) = (l1, l2, l3);

                // 单体温差过大
                (l1, l2, l3) = (alm.TempDifferenceProtection, alm.TempDifferenceAlarm, alm.TempDifferenceFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.CellTempDifferenceThreshold1!.Value, thr.CellTempDifferenceThreshold2!.Value, thr.CellTempDifferenceThreshold3!.Value, thr.CellTempDifferenceRecovery1!.Value, thr.CellTempDifferenceRecovery2!.Value, thr.CellTempDifferenceRecovery3!.Value, maxTempList.Max() - minTempList.Min());
                (alm.TempDifferenceProtection, alm.TempDifferenceAlarm, alm.TempDifferenceFault) = (l1, l2, l3);

                // SOC过低
                (l1, l2, l3) = (alm.LowSOCProtection, alm.LowSOCAlarm, alm.LowSOCFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.LowSOCTreshold1!.Value, thr.LowSOCTreshold2!.Value, thr.LowSOCTreshold3!.Value, thr.LowSOCRecovery1!.Value, thr.LowSOCRecovery2!.Value, thr.LowSOCRecovery3!.Value, (float)cs.MinPackSOC);
                (alm.LowSOCProtection, alm.LowSOCAlarm, alm.LowSOCFault) = (l1, l2, l3);

                // 充电温度过高
                (l1, l2, l3) = (alm.CellChargeHighTempProtection, alm.CellChargeHighTempAlarm, alm.CellChargeHighTempFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.ChargeHighTempThreshold1!.Value, thr.ChargeHighTempThreshold2!.Value, thr.ChargeHighTempThreshold3!.Value, thr.ChargeHighTempRecovery1!.Value, thr.ChargeHighTempRecovery2!.Value, thr.ChargeHighTempRecovery3!.Value, maxTempList.Max());
                (alm.CellChargeHighTempProtection, alm.CellChargeHighTempAlarm, alm.CellChargeHighTempFault) = (l1, l2, l3);

                // 充电温度过低
                (l1, l2, l3) = (alm.CellChargeLowTempProtection, alm.CellChargeLowTempAlarm, alm.CellChargeLowTempFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.ChargeLowTempThreshold1!.Value, thr.ChargeLowTempThreshold2!.Value, thr.ChargeLowTempThreshold3!.Value, thr.ChargeLowTempRecovery1!.Value, thr.ChargeLowTempRecovery2!.Value, thr.ChargeLowTempRecovery3!.Value, minTempList.Min());
                (alm.CellChargeLowTempProtection, alm.CellChargeLowTempAlarm, alm.CellChargeLowTempFault) = (l1, l2, l3);

                // 绝缘值过低
                (l1, l2, l3) = (alm.InsulationProtection, alm.InsulationAlarm, alm.InsulationFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.InsulationThreshold1!.Value, thr.InsulationThreshold2!.Value, thr.InsulationThreshold3!.Value, thr.InsulationRecovery1!.Value, thr.InsulationRecovery2!.Value, thr.InsulationRecovery3!.Value, m.Insulation!.Value);
                (alm.InsulationProtection, alm.InsulationAlarm, alm.InsulationFault) = (l1, l2, l3);

                // 放电温度过高
                (l1, l2, l3) = (alm.CellDischargeHighTempProtection, alm.CellDischargeHighTempAlarm, alm.CellDischargeHighTempFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.DischargeHighTempThreshold1!.Value, thr.DischargeHighTempThreshold2!.Value, thr.DischargeHighTempThreshold3!.Value, thr.DischargeHighTempRecovery1!.Value, thr.DischargeHighTempRecovery2!.Value, thr.DischargeHighTempRecovery3!.Value, maxTempList.Max());
                (alm.CellDischargeHighTempProtection, alm.CellDischargeHighTempAlarm, alm.CellDischargeHighTempFault) = (l1, l2, l3);

                // 放电温度过低
                (l1, l2, l3) = (alm.CellDischargeLowTempProtection, alm.CellDischargeLowTempAlarm, alm.CellDischargeLowTempFault);
                UpdateUnder(ref l1, ref l2, ref l3, thr.DischargeLowTempThreshold1!.Value, thr.DischargeLowTempThreshold2!.Value, thr.DischargeLowTempThreshold3!.Value, thr.DischargeLowTempRecovery1!.Value, thr.DischargeLowTempRecovery2!.Value, thr.DischargeLowTempRecovery3!.Value, minTempList.Min());
                (alm.CellDischargeLowTempProtection, alm.CellDischargeLowTempAlarm, alm.CellDischargeLowTempFault) = (l1, l2, l3);

                // 高压箱连接器温度过高
                (l1, l2, l3) = (alm.BatteryBoxBusbarHighTempProtection, alm.BatteryBoxBusbarHighTempAlarm, alm.BatteryBoxBusbarHighTempFault);
                UpdateOver(ref l1, ref l2, ref l3, thr.HVBHighTempThreshold1!.Value, thr.HVBHighTempThreshold2!.Value, thr.HVBHighTempThreshold3!.Value, thr.HVBHighTempRecovery1!.Value, thr.HVBHighTempRecovery2!.Value, thr.HVBHighTempRecovery3!.Value, 26.0f);
                (alm.BatteryBoxBusbarHighTempProtection, alm.BatteryBoxBusbarHighTempAlarm, alm.BatteryBoxBusbarHighTempFault) = (l1, l2, l3);
            }
        }

        // ── 三级告警状态机（使用局部 ref 变量）───────────────────────

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

        // ── 极值查找 ──────────────────────────────────────────────────

        private static void FindExtreme(RackState rack, int type,
            out float value, out int clusterId, out int packId, out int cellId)
        {
            value = 0; clusterId = 0; packId = 0; cellId = 0;
            if (rack?.ClusterStates == null || rack.ClusterStates.Count == 0) return;

            bool findMax = type is 1 or 3;
            Func<CellState, double> sel = type switch
            {
                1 or 2 => c => c.Voltage,
                3 or 4 => c => c.Temperature,
                _      => c => 0
            };

            double best = findMax ? double.MinValue : double.MaxValue;
            int bc = 0, bp = 0, be = 0;

            for (int i = 0; i < rack.ClusterStates.Count; i++)
            {
                var cluster = rack.ClusterStates[i];
                if (cluster?.PackStates == null) continue;
                for (int j = 0; j < cluster.PackStates.Count; j++)
                {
                    var pack = cluster.PackStates[j];
                    if (pack?.CellStates == null) continue;
                    for (int k = 0; k < pack.CellStates.Count; k++)
                    {
                        double v = sel(pack.CellStates[k]);
                        if ((findMax && v > best) || (!findMax && v < best))
                        { best = v; bc = i; bp = j; be = k; }
                    }
                }
            }
            value = (float)best; clusterId = bc; packId = bp; cellId = be;
        }
    }
}

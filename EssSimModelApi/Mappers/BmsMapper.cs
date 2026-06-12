using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>
    /// 将 ESS 物理模型数据映射到 BMS 接口数据对象（物理 → DTO，无副作用）。
    /// 保护评估与 Rack 故障回写由 <see cref="BmsRackProtection"/> 负责。
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
            }
        }

        /// <summary>向后兼容：委托至 <see cref="BmsRackProtection.UpdateUnder"/>。</summary>
        public static void UpdateUnder(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val) =>
            BmsRackProtection.UpdateUnder(ref l1, ref l2, ref l3, t1, t2, t3, r1, r2, r3, val);

        /// <summary>向后兼容：委托至 <see cref="BmsRackProtection.UpdateOver"/>。</summary>
        public static void UpdateOver(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val) =>
            BmsRackProtection.UpdateOver(ref l1, ref l2, ref l3, t1, t2, t3, r1, r2, r3, val);

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

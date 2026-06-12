using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class BatteryStack
    {
        [System.Runtime.Serialization.IgnoreDataMember]
        public float NominalEnergyKWh { get; set; } = 5016f;

        [System.Runtime.Serialization.IgnoreDataMember]
        public float MaxCRate { get; set; } = 0.5f;

        public int StackId { get; set; } // 堆编号

        /// <summary>一键并网脉冲命令：写 1 触发，处理后自动回 0。</summary>
        public ushort GridConnectCommand { get; set; }

        /// <summary>一键并网状态：0 未开始 / 1 进行中 / 2 成功 / 3 失败。</summary>
        public ushort GridConnectStatus { get; set; }

        /// <summary>BMS 与 PCS 直流侧是否已关联。</summary>
        public bool IsPcsLinked { get; set; }

        /// <summary>黑启动模式指令（param12）：写 1 进入，写 0 退出。</summary>
        public ushort BlackStartCommand { get; set; }

        /// <summary>进入黑启动模式是否成功（param5）：0 否 / 1 是。</summary>
        public ushort BlackStartEnterSuccess { get; set; }

        /// <summary>当前黑启动状态（param6）：3 已进入 / 4 进入失败 / 5 已退出。</summary>
        public ushort BlackStartStatus { get; set; }

        // 堆基本信息
        public float? TotalVoltage { get; set; } // 堆端总电压
        public float? Current { get; set; } // 堆端电流
        public float? Power { get; set; } // 堆端功率
        // 系统充放电状态 0-未充放电，1-放电，2-充电
        public int? ChargeDischargeStatus 
        {
            get
            {
                if (Current.HasValue)
                {
                    if (Current.Value > 0)
                        return 1; // 放电
                    else if (Current.Value < 0)
                        return 2; // 充电
                    else
                        return 0; // 未充放电
                }
                else
                {
                    return null; // 电流无效时返回null
                }
            }
        }
        public float? SOC { get; set; } // 堆SOC
        public float? SOH { get; set; } // 堆SOH
        public float? InsulationPlus { get; set; } // 正极对地绝缘值
        public float? InsulationMinus { get; set; } // 负极对地绝缘值
        public int? OperationStatus { get; set; } // 运行状态码
        public float Cycles { get; set; } // 循环次数
        public float res2 { get; set; } // 预留
        public float res3 { get; set; } // 预留
        public float res4 { get; set; } // 预留
        public float res5 { get; set; } // 预留


        // 堆内统计信息
        public float? MaxCellVoltage { get; set; } // 堆内最大单体电压
        public int? MaxCellVoltageClusterId { get; set; } // 所在簇编号
        public int? MaxCellVoltagePackId { get; set; } // 所在组编号
        public int? MaxCellVoltageCellId { get; set; } // 所在单体编号
        public float? MinCellVoltage { get; set; } // 堆内最小单体电压
        public int? MinCellVoltageClusterId { get; set; } // 所在簇编号
        public int? MinCellVoltagePackId { get; set; } // 所在组编号
        public int? MinCellVoltageCellId { get; set; } // 所在单体编号
        public float? AvgCellVoltage { get; set; } // 堆内单体平均电压
        public float? CellVoltageDiff { get; set; } // 堆内单体电压差

        public float? MaxCellTemp { get; set; } // 堆内最大单体温度
        public int? MaxCellTempClusterId { get; set; } // 所在簇编号
        public int? MaxCellTempPackId { get; set; } // 所在组编号
        public int? MaxCellTempCellId { get; set; } // 所在单体编号
        public float? MinCellTemp { get; set; } // 堆内最小单体温度
        public int? MinCellTempClusterId { get; set; } // 所在簇编号
        public int? MinCellTempPackId { get; set; } // 所在组编号
        public int? MinCellTempCellId { get; set; } // 所在单体编号
        public float? AvgCellTemp { get; set; } // 堆内单体平均温度
        public float? CellTempDiff { get; set; } // 堆内单体温差

        public float? MaxCellSOC { get; set; } // 堆内最大单体SOC
        public float? MinCellSOC { get; set; } // 堆内最小单体SOC

        // 电池簇管理
        public int? ManagedClusterCount { get; set; } // 管理的簇数量
        public int? ManagedCellCount { get; set; } // 管理的单体数量
        public int? ManagedTempSensorCount { get; set; } // 管理的温感数量

        // 能量统计
        public float? CumulativeChargeEnergy { get; set; } // 堆累计充电电量
        public float? CumulativeDischargeEnergy { get; set; } // 堆累计放电电量
        // public float? SingleChargeEnergy { get; set; } // 堆本次充电电量
        // public float? SingleDischargeEnergy { get; set; } // 堆本次放电电量
        // public float? DailyChargeEnergy { get; set; } // 堆当日充电电量
        // public float? DailyDischargeEnergy { get; set; } // 堆当日放电电量
        // public float? AvailableChargeEnergy { get; set; } // 堆可用充电容量
        // public float? AvailableDischargeEnergy { get; set; } // 堆可用放电容量

        // 限制参数
        public float? MaxChargePower // 最大允许充电功率
        { 
            get
            {
                if (!SOC.HasValue) return null;
                float basePower = Math.Max(0f, NominalEnergyKWh) * Math.Max(0f, MaxCRate);
                return basePower * GetChargePowerFactor(SOC.Value);
            }
        } 

        public float? MaxDischargePower // 最大允许放电功率
        { 
            get
            {
                if (!SOC.HasValue) return null;
                float basePower = Math.Max(0f, NominalEnergyKWh) * Math.Max(0f, MaxCRate);
                return basePower * GetDischargePowerFactor(SOC.Value);
            }
        } 

        public float? MaxChargeCurrent  // 最大允许充电电流
        {
            get
            {
                // 根据当前的最大充电功率和当前电压计算最大充电电流
                if (MaxChargePower.HasValue && TotalVoltage.HasValue && TotalVoltage.Value > 0)
                {
                    return MaxChargePower.Value * 1000f / TotalVoltage.Value; // 转换为安培
                }
                else
                {
                    return null;
                }
            }
        }

        public float? MaxDischargeCurrent   // 最大允许放电电流
        {
            get
            {
                // 根据当前的最大放电功率和当前电压计算最大放电电流
                if (MaxDischargePower.HasValue && TotalVoltage.HasValue && TotalVoltage.Value > 0)
                {
                    return MaxDischargePower.Value * 1000f / TotalVoltage.Value; // 转换为安培
                }
                else
                {
                    return null;
                }
            }
        } 

        public float? AvailableChargeCapacity 
        { 
            get
            {
                if (!SOC.HasValue) return null;
                return NominalEnergyKWh * (1 - SOC.Value);
            }
        } // 可用充电容量
        public float? AvailableDischargeCapacity 
        { 
            get
            {
                if (!SOC.HasValue) return null;
                return NominalEnergyKWh * SOC.Value;
            }
        } // 可用放电容量

        private static float GetChargePowerFactor(float soc)
        {
            if (soc < 0.8f) return 1.0f;
            if (soc < 0.85f) return 1.0f - (soc - 0.8f) / 0.05f * 0.5f;
            if (soc < 0.9f) return 0.5f - (soc - 0.85f) / 0.05f * 0.25f;
            return 0.0f;
        }

        private static float GetDischargePowerFactor(float soc)
        {
            if (soc > 0.2f) return 1.0f;
            if (soc > 0.15f) return 0.5f + (soc - 0.15f) / 0.05f * 0.5f;
            if (soc > 0.1f) return 0.25f + (soc - 0.1f) / 0.05f * 0.25f;
            return 0.0f;
        }

        // 告警信息
        public bool? BMSSystemChannelStatus { get; set; } // BMS系统通道状态
        public ushort BMSProtectionSummary 
        { 
            get
            {
                ushort res = 0;
                // 统计所有簇的总电压过压三级保护状态
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    foreach (var cluster in Cluseter)
                    {
                        res |= cluster.Alarms.RackProtectionSummary1;
                    }
                }
                return res;
            }
        } // 一级告警汇总

        public ushort BMSAlarmSummary 
        { 
            get
            {
                ushort res = 0;
                // 统计所有簇的总电压过压三级告警状态
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    foreach (var cluster in Cluseter)
                    {
                        res |= cluster.Alarms.RackAlarmSummary1;
                    }
                }
                return res;
            }
        } // 二级告警汇总

        public ushort BMSFaultSummary 
        { 
            get
            {
                ushort res = 0;
                // 统计所有簇的总电压过压三级故障状态
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    foreach (var cluster in Cluseter)
                    {
                        res |= cluster.Alarms.RackFaultSummary1;
                    }
                }
                return res;
            }
        } // 三级告警汇总

        public bool IsChargeFault
        {
            get
            {
                // 判断是否存在充电故障
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    foreach (var cluster in Cluseter)
                    {
                        if (cluster.Alarms.IsChargeFault)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        } // 是否存在充电故障

        public bool IsDischargeFault
        {
            get
            {
                // 判断是否存在放电故障
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    foreach (var cluster in Cluseter)
                    {
                        if (cluster.Alarms.IsDischargeFault)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        } // 是否存在放电故障

        // 簇间差异
        public float? ClusterVoltageDiff 
        { 
            get
            {
                // 计算簇间压差值
                if (MaxCellVoltage.HasValue && MinCellVoltage.HasValue)
                {
                    return MaxCellVoltage.Value - MinCellVoltage.Value;
                }
                else
                {
                    return null;
                }
            }
        } // 簇间压差值

        public bool? ClusterVoltageDiffAlarm { get; set; } // 簇间压差报警
        public float? ClusterCurrentDiff 
        { 
            get
            {
                // 计算簇间电流差值
                if (Cluseter != null && Cluseter.Count > 0)
                {
                    float? maxCurrent = null;
                    float? minCurrent = null;
                    foreach (var cluster in Cluseter)
                    {
                        if (cluster.Measurements.Current.HasValue)
                        {
                            if (!maxCurrent.HasValue || cluster.Measurements.Current.Value > maxCurrent.Value)
                            {
                                maxCurrent = cluster.Measurements.Current.Value;
                            }
                            if (!minCurrent.HasValue || cluster.Measurements.Current.Value < minCurrent.Value)
                            {
                                minCurrent = cluster.Measurements.Current.Value;
                            }
                        }
                    }
                    if (maxCurrent.HasValue && minCurrent.HasValue)
                    {
                        return maxCurrent.Value - minCurrent.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        } // 簇间电流差值
        public bool? ClusterCurrentImbalanceAlarm { get; set; } // 簇间电流不平衡报警

        // 包含的电池簇
        public List<BatteryCluster> Cluseter { get; set; } = new List<BatteryCluster>(); // 所含簇列表

        // 预留字段
        public List<float?> Reserved { get; set; } = new List<float?>(); // 预留
    }
}

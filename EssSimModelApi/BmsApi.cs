using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EssSimulator.EssDeviceSimModel;

namespace EssSimulator.EssSimModelApi
{
    using System;
    using System.Collections.Generic;

    namespace BatteryManagementSystem
    {
        // 电池簇基础测量数据
        public class ClusterBasicMeasurements
        {
            [System.Runtime.Serialization.IgnoreDataMember]
            public float NominalEnergyKWh { get; set; } = 5016f / 12f;

            [System.Runtime.Serialization.IgnoreDataMember]
            public float MaxCRate { get; set; } = 0.5f;

            public float? TotalVoltage { get; set; } // 簇端总电压
            public float? Current { get; set; } // 簇端电流
            public float? Power { get; set; } // 簇端功率
            public float? SOC { get; set; } // 荷电状态
            public float? SOH { get; set; } // 健康状态
            public float? Insulation { get; set; } // 绝缘值
            public float? InsulationPlus { get; set; } // 正极对地绝缘值
            public float? InsulationMinus { get; set; } // 负极对地绝缘值
            public int? OperationStatus { get; set; } // 运行状态码
            // 电压相关
            public float? MaxCellVoltage { get; set; } // 最大单体电压
            public int? MaxCellVoltageId { get; set; } // 最大单体编号
            public float? MinCellVoltage { get; set; } // 最小单体电压
            public int? MinCellVoltageId { get; set; } // 最小单体编号
            public float? AvgCellVoltage { get; set; } // 单体平均电压
            public float? CellVoltageSum { get; set; } // 单体累加和总压

            // 温度相关
            public float? MaxCellTemp { get; set; } // 最大单体温度
            public int? MaxCellTempId { get; set; } // 最大温度单体编号
            public float? MinCellTemp { get; set; } // 最小单体温度
            public int? MinCellTempId { get; set; } // 最小温度单体编号
            public float? AvgCellTemp { get; set; } // 单体平均温度
            public float? HVB1Temp { get; set; } // 高压箱温度1
            public float? HVB2Temp { get; set; } // 高压箱温度2

            // SOC相关
            public float? MaxCellSOC { get; set; } // 最大单体SOC
            public float? MinCellSOC { get; set; } // 最小单体SOC

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

            public float? MaxChargeCurrent // 最大充电电流
            { 
                get
                {
                    // 根据最大充电功率和当前总电压计算最大充电电流
                    if (MaxChargePower.HasValue && TotalVoltage.HasValue && TotalVoltage.Value > 0)
                    {
                        return MaxChargePower.Value * 1000 / TotalVoltage.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            } 

            public float? MaxDischargeCurrent // 最大放电电流
            { 
                get
                {
                    // 根据最大放电功率和当前总电压计算最大放电电流
                    if (MaxDischargePower.HasValue && TotalVoltage.HasValue && TotalVoltage.Value > 0)
                    {
                        return MaxDischargePower.Value * 1000 / TotalVoltage.Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            } 

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
        }

        // 电池簇告警数据
        public class ClusterAlarms
        {
            public ClusterAlarms()
            {
                MildAlarm = false;
                ModerateAlarm = false;
                SevereAlarm = false;
                UndervoltageAlarm = false;
                UndervoltageFault = false;
                UndervoltageProtection = false;
                OvervoltageAlarm = false;
                OvervoltageFault = false;
                OvervoltageProtection = false;
                ChargeOvercurrentAlarm = false;
                ChargeOvercurrentFault = false;
                ChargeOvercurrentProtection = false;
                DischargeOvercurrentAlarm = false;
                DischargeOvercurrentFault = false;
                DischargeOvercurrentProtection = false;
                InsulationAlarm = false;
                InsulationFault = false;
                InsulationProtection = false;
                CellUnderVoltageAlarm = false;
                CellUnderVoltageFault = false;
                CellUnderVoltageProtection = false;
                CellOverVoltageAlarm = false;
                CellOverVoltageFault = false;
                CellOverVoltageProtection = false;
                VoltageDifferenceAlarm = false;
                VoltageDifferenceFault = false;
                VoltageDifferenceProtection = false;
                resv1 = false;
                resv2 = false;
                resv3 = false;
                resv4 = false;
                resv5 = false;
                resv6 = false;
                TempDifferenceAlarm = false;
                TempDifferenceFault = false;
                TempDifferenceProtection = false;
                LowSOCAlarm = false;
                LowSOCFault = false;
                LowSOCProtection = false;
                resv21 = false;
                resv22 = false;
                resv23 = false;
                resv24 = false;
                resv25 = false;
                resv26 = false;
                TerminalHighTempAlarm = false;
                TerminalHighTempFault = false;
                TerminalHighTempProtection = false;
                CellChargeLowTempAlarm = false;
                CellChargeLowTempFault = false;
                CellChargeLowTempProtection = false;
                CellChargeHighTempAlarm = false;
                CellChargeHighTempFault = false;
                CellChargeHighTempProtection = false;
                CellDischargeLowTempAlarm = false;
                CellDischargeLowTempFault = false;
                CellDischargeLowTempProtection = false;
                CellDischargeHighTempAlarm = false;
                CellDischargeHighTempFault = false;
                CellDischargeHighTempProtection = false;
                HVBHighTempAlarm = false;
                HVBHighTempFault = false;
                HVBHighTempProtection = false;
                TempRiseAlarm = false;
                TempRiseFault = false;
                TempRiseProtection = false;
                TempSamplingFault = false;
                VoltageSamplingFault = false;
                MasterCommFault = false;
                SlaveCommFault = false;
                MainPositiveContactor = false;
                MainNegativeContactor = false;
                resv31 = false;
                ChargeProhibited = false;
                DischargeProhibited = false;
                BatteryBoxOvervoltageProtection = false;
                BatteryBoxOvervoltageAlarm = false;
                BatteryBoxOvervoltageFault = false;
                BatteryBoxUndervoltageProtection = false;
                BatteryBoxUndervoltageAlarm = false;
                BatteryBoxUndervoltageFault = false;
                BatteryBoxTempDifferenceAlarm = false;
                BatteryBoxTempDifferenceFault = false;
                BatteryBoxTempDifferenceProtection = false;
                BatteryBoxPositivePoleTempDifferenceAlarm = false;
                BatteryBoxPositivePoleTempDifferenceFault = false;
                BatteryBoxPositivePoleTempDifferenceProtection = false;
                BatteryBoxNegativePoleTempDifferenceAlarm = false;
                BatteryBoxNegativePoleTempDifferenceFault = false;
                BatteryBoxNegativePoleTempDifferenceProtection = false;
                BatteryBoxBusbarHighTempAlarm = false;
                BatteryBoxBusbarHighTempFault = false;
                BatteryBoxBusbarHighTempProtection = false;
                BatteryBoxVoltageExtremaDifferenceAlarm = false;
                BatteryBoxVoltageExtremaDifferenceFault = false;
                BatteryBoxVoltageExtremaDifferenceProtection = false;
            }

            // 告警级别
            public bool? MildAlarm { get; set; } // 轻微告警
            public bool? ModerateAlarm { get; set; } // 中等告警
            public bool? SevereAlarm { get; set; } // 严重告警

            // 电压相关告警
            public bool? UndervoltageAlarm { get; set; } // 簇欠压报警
            public bool? UndervoltageFault { get; set; } // 簇欠压故障
            public bool? UndervoltageProtection { get; set; } // 簇欠压保护动作
            public bool? OvervoltageAlarm { get; set; } // 簇过压报警
            public bool? OvervoltageFault { get; set; } // 簇过压故障
            public bool? OvervoltageProtection { get; set; } // 簇过压保护动作

            // 电流相关告警
            public bool? ChargeOvercurrentAlarm { get; set; } // 充电过流报警
            public bool? ChargeOvercurrentFault { get; set; } // 充电过流故障
            public bool? ChargeOvercurrentProtection { get; set; } // 充电过流保护
            public bool? DischargeOvercurrentAlarm { get; set; } // 放电过流报警
            public bool? DischargeOvercurrentFault { get; set; } // 放电过流故障
            public bool? DischargeOvercurrentProtection { get; set; } // 放电过流保护

            // 绝缘相关
            public bool? InsulationAlarm { get; set; } // 绝缘报警
            public bool? InsulationFault { get; set; } // 绝缘故障
            public bool? InsulationProtection { get; set; } // 绝缘保护

            //单体欠压
            public bool? CellUnderVoltageAlarm { get; set; } // 单体欠压报警
            public bool? CellUnderVoltageFault { get; set; } // 单体欠压故障
            public bool? CellUnderVoltageProtection { get; set; } // 单体欠压保护
            //单体过压
            public bool? CellOverVoltageAlarm { get; set; } // 单体过压报警
            public bool? CellOverVoltageFault { get; set; } // 单体过压故障
            public bool? CellOverVoltageProtection { get; set; } // 单体过压保护

            // 压差相关
            public bool? VoltageDifferenceAlarm { get; set; } // 单体压差报警
            public bool? VoltageDifferenceFault { get; set; } // 单体压差故障
            public bool? VoltageDifferenceProtection { get; set; } // 单体压差保护

            public bool? resv1 { get; set; } // 预留
            public bool? resv2 { get; set; } // 预留
            public bool? resv3 { get; set; } // 预留
            public bool? resv4 { get; set; } // 预留
            public bool? resv5 { get; set; } // 预留
            public bool? resv6 { get; set; } // 预留

            // 温差相关
            public bool? TempDifferenceAlarm { get; set; } // 单体温差报警
            public bool? TempDifferenceFault { get; set; } // 单体温差故障
            public bool? TempDifferenceProtection { get; set; } // 单体温差保护

            // SOC相关
            public bool? LowSOCAlarm { get; set; } // SOC过低报警
            public bool? LowSOCFault { get; set; } // SOC过低故障
            public bool? LowSOCProtection { get; set; } // SOC过低保护
            // public bool? HighSOCAlarm { get; set; } // SOC过高报警
            // public bool? HighSOCFault { get; set; } // SOC过高故障
            // public bool? HighSOCProtection { get; set; } // SOC过高保护
            public bool? resv21 { get; set; } // 预留
            public bool? resv22 { get; set; } // 预留
            public bool? resv23 { get; set; } // 预留
            public bool? resv24 { get; set; } // 预留
            public bool? resv25 { get; set; } // 预留
            public bool? resv26 { get; set; } // 预留

            // 温度相关
            public bool? TerminalHighTempAlarm { get; set; } // 端子高温报警
            public bool? TerminalHighTempFault { get; set; } // 端子高温故障
            public bool? TerminalHighTempProtection { get; set; } // 端子高温保护

            public bool? CellChargeLowTempAlarm { get; set; } // 充电低温报警
            public bool? CellChargeLowTempFault { get; set; } // 充电低温故障
            public bool? CellChargeLowTempProtection { get; set; } // 充电低温保护
            public bool? CellChargeHighTempAlarm { get; set; } // 充电高温报警
            public bool? CellChargeHighTempFault { get; set; } // 充电高温故障
            public bool? CellChargeHighTempProtection { get; set; } // 充电高温保护
            public bool? CellDischargeLowTempAlarm { get; set; } // 放电低温报警
            public bool? CellDischargeLowTempFault { get; set; } // 放电低温故障
            public bool? CellDischargeLowTempProtection { get; set; } // 放电低温保护
            public bool? CellDischargeHighTempAlarm { get; set; } // 放电高温报警
            public bool? CellDischargeHighTempFault { get; set; } // 放电高温故障
            public bool? CellDischargeHighTempProtection { get; set; } // 放电高温保护
            public bool? HVBHighTempAlarm { get; set; } // 高压箱连接器高温报警
            public bool? HVBHighTempFault { get; set; } // 高压箱连接器高温故障
            public bool? HVBHighTempProtection { get; set; } // 高压箱连接器高温保护

            // 温升相关
            public bool? TempRiseAlarm { get; set; } // 温升报警
            public bool? TempRiseFault { get; set; } // 温升故障
            public bool? TempRiseProtection { get; set; } // 温升保护

            // 采样故障
            public bool? TempSamplingFault { get; set; } // 温度采样故障
            public bool? VoltageSamplingFault { get; set; } // 电压采样故障

            // 通信故障
            public bool? MasterCommFault { get; set; } // 主机通信故障
            public bool? SlaveCommFault { get; set; } // 从机通信故障

            // 接触器状态
            public bool? MainPositiveContactor { get; set; } // 主正接触器状态
            public bool? MainNegativeContactor { get; set; } // 主负接触器状态

            public bool? resv31 { get; set; } // 预留

            // 充放电禁止
            public bool? ChargeProhibited { get; set; } // 禁止充电
            public bool? DischargeProhibited { get; set; } // 禁止放电

            // 电池箱相关
            public bool? BatteryBoxOvervoltageProtection { get; set; } // 电池箱过压保护
            public bool? BatteryBoxOvervoltageAlarm { get; set; } // 电池箱过压报警
            public bool? BatteryBoxOvervoltageFault { get; set; } // 电池箱过压故障
            public bool? BatteryBoxUndervoltageProtection { get; set; } // 电池箱欠压保护
            public bool? BatteryBoxUndervoltageAlarm { get; set; } // 电池箱欠压报警
            public bool? BatteryBoxUndervoltageFault { get; set; } // 电池箱欠压故障
            // 电池箱温差
            public bool? BatteryBoxTempDifferenceAlarm { get; set; } // 电池箱温差报警
            public bool? BatteryBoxTempDifferenceFault { get; set; } // 电池箱温差故障
            public bool? BatteryBoxTempDifferenceProtection { get; set; } // 电池箱温差保护

            // 电池箱正极柱温差
            public bool? BatteryBoxPositivePoleTempDifferenceAlarm { get; set; } // 电池箱正极柱温差报警
            public bool? BatteryBoxPositivePoleTempDifferenceFault { get; set; } // 电池箱正极柱温差故障
            public bool? BatteryBoxPositivePoleTempDifferenceProtection { get; set; } // 电池箱正极柱温差保护

            // 电池箱负极柱温差
            public bool? BatteryBoxNegativePoleTempDifferenceAlarm { get; set; } // 电池箱负极柱温差报警
            public bool? BatteryBoxNegativePoleTempDifferenceFault { get; set; } // 电池箱负极柱温差故障
            public bool? BatteryBoxNegativePoleTempDifferenceProtection { get; set; } // 电池箱负极柱温差保护

            // 电池箱铜牌高温
            public bool? BatteryBoxBusbarHighTempAlarm { get; set; } // 电池箱铜排高温报警
            public bool? BatteryBoxBusbarHighTempFault { get; set; } // 电池箱铜排高温故障
            public bool? BatteryBoxBusbarHighTempProtection { get; set; } // 电池箱铜排高温保护

            // 电池箱电压极差
            public bool? BatteryBoxVoltageExtremaDifferenceAlarm { get; set; } // 电池箱电压极差报警
            public bool? BatteryBoxVoltageExtremaDifferenceFault { get; set; } // 电池箱电压极差故障
            public bool? BatteryBoxVoltageExtremaDifferenceProtection { get; set; } // 电池箱电压极差保护

            // rack保护汇总1，包含簇过压保护，簇欠压保护，单体过压保护，单体欠压保护，放电过流保护，充电过流保护，放电过温保护，放电欠温保护，充电过温保护，充电欠温保护，绝缘保护，端子高温保护，高压箱连接器高温保护，单体压差保护，单体温差保护，SOC过低保护
            public UInt16 RackProtectionSummary1
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(OvervoltageProtection) << 0);
                    summary |= (UInt16)(ToBit(UndervoltageProtection) << 1);
                    summary |= (UInt16)(ToBit(CellOverVoltageProtection) << 2);
                    summary |= (UInt16)(ToBit(CellUnderVoltageProtection) << 3);
                    summary |= (UInt16)(ToBit(DischargeOvercurrentProtection) << 4);
                    summary |= (UInt16)(ToBit(ChargeOvercurrentProtection) << 5);
                    summary |= (UInt16)(ToBit(CellDischargeHighTempProtection) << 6);
                    summary |= (UInt16)(ToBit(CellDischargeLowTempProtection) << 7);
                    summary |= (UInt16)(ToBit(CellChargeHighTempProtection) << 8);
                    summary |= (UInt16)(ToBit(CellChargeLowTempProtection) << 9);
                    summary |= (UInt16)(ToBit(InsulationProtection) << 10);
                    summary |= (UInt16)(ToBit(TerminalHighTempProtection) << 11);
                    summary |= (UInt16)(ToBit(HVBHighTempProtection) << 12);
                    summary |= (UInt16)(ToBit(VoltageDifferenceProtection) << 13);
                    summary |= (UInt16)(ToBit(TempDifferenceProtection) << 14);
                    summary |= (UInt16)(ToBit(LowSOCProtection) << 15);
                    return summary;
                }
            }

            // 同上汇总报警
            public UInt16 RackAlarmSummary1
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(OvervoltageAlarm) << 0);
                    summary |= (UInt16)(ToBit(UndervoltageAlarm) << 1);
                    summary |= (UInt16)(ToBit(CellOverVoltageAlarm) << 2);
                    summary |= (UInt16)(ToBit(CellUnderVoltageAlarm) << 3);
                    summary |= (UInt16)(ToBit(DischargeOvercurrentAlarm) << 4);
                    summary |= (UInt16)(ToBit(ChargeOvercurrentAlarm) << 5);
                    summary |= (UInt16)(ToBit(CellDischargeHighTempAlarm) << 6);
                    summary |= (UInt16)(ToBit(CellDischargeLowTempAlarm) << 7);
                    summary |= (UInt16)(ToBit(CellChargeHighTempAlarm) << 8);
                    summary |= (UInt16)(ToBit(CellChargeLowTempAlarm) << 9);
                    summary |= (UInt16)(ToBit(InsulationAlarm) << 10);
                    summary |= (UInt16)(ToBit(TerminalHighTempAlarm) << 11);
                    summary |= (UInt16)(ToBit(HVBHighTempAlarm) << 12);
                    summary |= (UInt16)(ToBit(VoltageDifferenceAlarm) << 13);
                    summary |= (UInt16)(ToBit(TempDifferenceAlarm) << 14);
                    summary |= (UInt16)(ToBit(LowSOCAlarm) << 15);
                    return summary;
                }
            }

            // 同上汇总故障
            public UInt16 RackFaultSummary1
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(OvervoltageFault) << 0);
                    summary |= (UInt16)(ToBit(UndervoltageFault) << 1);
                    summary |= (UInt16)(ToBit(CellOverVoltageFault) << 2);
                    summary |= (UInt16)(ToBit(CellUnderVoltageFault) << 3);
                    summary |= (UInt16)(ToBit(DischargeOvercurrentFault) << 4);
                    summary |= (UInt16)(ToBit(ChargeOvercurrentFault) << 5);
                    summary |= (UInt16)(ToBit(CellDischargeHighTempFault) << 6);
                    summary |= (UInt16)(ToBit(CellDischargeLowTempFault) << 7);
                    summary |= (UInt16)(ToBit(CellChargeHighTempFault) << 8);
                    summary |= (UInt16)(ToBit(CellChargeLowTempFault) << 9);
                    summary |= (UInt16)(ToBit(InsulationFault) << 10);
                    summary |= (UInt16)(ToBit(TerminalHighTempFault) << 11);
                    summary |= (UInt16)(ToBit(HVBHighTempFault) << 12);
                    summary |= (UInt16)(ToBit(VoltageDifferenceFault) << 13);
                    summary |= (UInt16)(ToBit(TempDifferenceFault) << 14);
                    summary |= (UInt16)(ToBit(LowSOCFault) << 15);
                    return summary;
                }
            }

            // 汇总充电故障
            public bool IsChargeFault
            {
                get
                {
                    return ChargeOvercurrentFault == true ||
                           CellChargeLowTempFault == true ||
                           CellChargeHighTempFault == true ||
                           CellOverVoltageFault == true ||
                           OvervoltageFault == true;
                }
            }

            // 汇总放电故障
            public bool IsDischargeFault
            {
                get
                {
                    return DischargeOvercurrentFault == true ||
                           CellDischargeLowTempFault == true ||
                           CellDischargeHighTempFault == true ||
                           LowSOCFault == true ||
                           CellUnderVoltageFault == true ||
                           UndervoltageFault == true;
                }
            }

            // rack保护汇总2，包含电池箱过压保护，电池箱欠压保护，电池箱温差保护，电池箱正极柱温差保护，电池箱负极柱温差保护，电池箱铜排高温保护，电池箱电压极差保护
            public UInt16 RackProtectionSummary2
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(BatteryBoxOvervoltageProtection) << 0);
                    summary |= (UInt16)(ToBit(BatteryBoxUndervoltageProtection) << 1);
                    summary |= (UInt16)(ToBit(BatteryBoxTempDifferenceProtection) << 2);
                    summary |= (UInt16)(ToBit(BatteryBoxPositivePoleTempDifferenceProtection) << 3);
                    summary |= (UInt16)(ToBit(BatteryBoxNegativePoleTempDifferenceProtection) << 4);
                    summary |= (UInt16)(ToBit(BatteryBoxBusbarHighTempProtection) << 5);
                    summary |= (UInt16)(ToBit(BatteryBoxVoltageExtremaDifferenceProtection) << 6);
                    return summary;
                }
            }

            // 同上汇总报警
            public UInt16 RackAlarmSummary2
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(BatteryBoxOvervoltageAlarm) << 0);
                    summary |= (UInt16)(ToBit(BatteryBoxUndervoltageAlarm) << 1);
                    summary |= (UInt16)(ToBit(BatteryBoxTempDifferenceAlarm) << 2);
                    summary |= (UInt16)(ToBit(BatteryBoxPositivePoleTempDifferenceAlarm) << 3);
                    summary |= (UInt16)(ToBit(BatteryBoxNegativePoleTempDifferenceAlarm) << 4);
                    summary |= (UInt16)(ToBit(BatteryBoxBusbarHighTempAlarm) << 5);
                    summary |= (UInt16)(ToBit(BatteryBoxVoltageExtremaDifferenceAlarm) << 6);
                    return summary;
                }
            }

            // 同上汇总故障
            public UInt16 RackFaultSummary2
            {
                get
                {
                    UInt16 summary = 0;
                    summary |= (UInt16)(ToBit(BatteryBoxOvervoltageFault) << 0);
                    summary |= (UInt16)(ToBit(BatteryBoxUndervoltageFault) << 1);
                    summary |= (UInt16)(ToBit(BatteryBoxTempDifferenceFault) << 2);
                    summary |= (UInt16)(ToBit(BatteryBoxPositivePoleTempDifferenceFault) << 3);
                    summary |= (UInt16)(ToBit(BatteryBoxNegativePoleTempDifferenceFault) << 4);
                    summary |= (UInt16)(ToBit(BatteryBoxBusbarHighTempFault) << 5);
                    summary |= (UInt16)(ToBit(BatteryBoxVoltageExtremaDifferenceFault) << 6);
                    return summary;
                }
            }

            private static UInt16 ToBit(bool? flag)
            {
                return flag == true ? (UInt16)1 : (UInt16)0;
            }
        }

        // 电池簇单体电压数据
        public class ClusterBasicCellVoltages
        {
            // 使用字典存储单体电压，键为单体编号，值为电压值
            public Dictionary<int, float?> CellVoltages { get; set; } = new Dictionary<int, float?>(); // 单体编号→电压

            // 也可以使用数组，根据实际需要选择
             //public float?[] CellVoltageArray { get; set; } = new float?[416];
        }

        // 电池簇单体温度数据
        public class ClusterBasicCellTemperatures
        {
            // 单体温度
            public Dictionary<int, float?> CellTemperatures { get; set; } = new Dictionary<int, float?>(); // 单体编号→温度

            // 极柱温度
            public Dictionary<int, float?> PositivePoleTemperatures { get; set; } = new Dictionary<int, float?>(); // 正 极柱编号→温度
            public Dictionary<int, float?> NegativePoleTemperatures { get; set; } = new Dictionary<int, float?>(); // 负 极柱编号→温度
        }

        // 完整的电池簇数据
        public class BatteryCluster
        {
            public int ClusterId { get; set; } // 簇编号
            public ClusterBasicMeasurements Measurements { get; set; } = new ClusterBasicMeasurements(); // 基础测量
            public ClusterAlarms Alarms { get; set; } = new ClusterAlarms(); // 告警状态
            public ClusterBasicCellVoltages ClusterCellVoltages { get; set; } = new ClusterBasicCellVoltages(); // 单体电压
            public ClusterBasicCellTemperatures ClusterCellTemperatures { get; set; } = new ClusterBasicCellTemperatures(); // 单体温度
            public ClusterThresholds Thresholds { get; set; } = new ClusterThresholds(); // 阈值与恢复阈值
        }

        // 电池簇的保护阈值和恢复阈值
        public class ClusterThresholds
        {
            public ClusterThresholds()
            {
                // 单体欠压阈值: 保护/告警/故障
                CellUndervoltageThreshold1 = 3.1f;
                CellUndervoltageThreshold2 = 2.9f;
                CellUndervoltageThreshold3 = 2.6f;

                // 单体过压阈值: 保护/告警/故障
                CellOvervoltageThreshold1 = 3.4f;
                CellOvervoltageThreshold2 = 3.6f;
                CellOvervoltageThreshold3 = 3.9f;

                // 簇欠压阈值: 保护/告警/故障
                UndervoltageThreshold1 = 416*3.1f;
                UndervoltageThreshold2 = 416*2.9f;
                UndervoltageThreshold3 = 416*2.6f;

                // 簇过压阈值: 保护/告警/故障
                OvervoltageThreshold1 = 416*3.4f;
                OvervoltageThreshold2 = 416*3.6f;
                OvervoltageThreshold3 = 416*3.9f;

                // 充电过流阈值: 保护/告警/故障
                ChargeOvercurrentThreshold1 = 300f;
                ChargeOvercurrentThreshold2 = 350f;
                ChargeOvercurrentThreshold3 = 400f;

                // 放电过流阈值: 保护/告警/故障
                DischargeOvercurrentThreshold1 = 300f;
                DischargeOvercurrentThreshold2 = 350f;
                DischargeOvercurrentThreshold3 = 400f;

                // 充电高温阈值: 保护/告警/故障
                ChargeHighTempThreshold1 = 55f;
                ChargeHighTempThreshold2 = 60f;
                ChargeHighTempThreshold3 = 65f;

                // 充电低温阈值: 保护/告警/故障
                ChargeLowTempThreshold1 = 15f;
                ChargeLowTempThreshold2 = 10f;
                ChargeLowTempThreshold3 = 5f;

                // 放电高温阈值: 保护/告警/故障
                DischargeHighTempThreshold1 = 55f;
                DischargeHighTempThreshold2 = 60f;
                DischargeHighTempThreshold3 = 65f;

                // 放电低温阈值: 保护/告警/故障
                DischargeLowTempThreshold1 = 15f;
                DischargeLowTempThreshold2 = 10f;
                DischargeLowTempThreshold3 = 5f;

                // 低SOC阈值: 保护/告警/故障
                LowSOCTreshold1 = 0.15f;
                LowSOCTreshold2 = 0.10f;
                LowSOCTreshold3 = 0.05f;

                // 端子高温阈值: 保护/告警/故障
                PoleHighTempThreshold1 = 65f;
                PoleHighTempThreshold2 = 70f;
                PoleHighTempThreshold3 = 75f;

                // 绝缘阈值: 保护/告警/故障
                InsulationThreshold1 = 500f;
                InsulationThreshold2 = 300f;
                InsulationThreshold3 = 100f;

                // 单体压差阈值: 保护/告警/故障
                CellVoltageDifferenceThreshold1 = 0.2f;
                CellVoltageDifferenceThreshold2 = 0.25f;
                CellVoltageDifferenceThreshold3 = 0.3f;

                // 簇压差阈值: 保护/告警/故障
                TotalVoltageDifferenceThreshold1 = 10f;
                TotalVoltageDifferenceThreshold2 = 15f;
                TotalVoltageDifferenceThreshold3 = 20f;

                // 单体温差阈值: 保护/告警/故障
                CellTempDifferenceThreshold1 = 3f;
                CellTempDifferenceThreshold2 = 5f;
                CellTempDifferenceThreshold3 = 7f;

                // 高压箱连接器高温阈值: 保护/告警/故障
                HVBHighTempThreshold1 = 65f;
                HVBHighTempThreshold2 = 70f;
                HVBHighTempThreshold3 = 75f;

                // 单体欠压恢复阈值: 保护/告警/故障
                CellUndervoltageRecovery1 = CellUndervoltageThreshold1.Value + 0.1f;
                CellUndervoltageRecovery2 = CellUndervoltageThreshold2.Value + 0.1f;
                CellUndervoltageRecovery3 = CellUndervoltageThreshold3.Value + 0.1f;
                
                // 单体过压恢复阈值: 保护/告警/故障
                CellOvervoltageRecovery1 = CellOvervoltageThreshold1.Value - 0.1f;
                CellOvervoltageRecovery2 = CellOvervoltageThreshold2.Value - 0.1f;
                CellOvervoltageRecovery3 = CellOvervoltageThreshold3.Value - 0.1f;

                // 簇欠压恢复阈值: 保护/告警/故障
                UndervoltageRecovery1 = UndervoltageThreshold1.Value + 5f;
                UndervoltageRecovery2 = UndervoltageThreshold2.Value + 5f;
                UndervoltageRecovery3 = UndervoltageThreshold3.Value + 5f;

                // 簇过压恢复阈值: 保护/告警/故障
                OvervoltageRecovery1 = OvervoltageThreshold1.Value - 5f;
                OvervoltageRecovery2 = OvervoltageThreshold2.Value - 5f;
                OvervoltageRecovery3 = OvervoltageThreshold3.Value - 5f;

                // 充电过流恢复阈值: 保护/告警/故障
                ChargeOvercurrentRecovery1 = ChargeOvercurrentThreshold1.Value - 10f;
                ChargeOvercurrentRecovery2 = ChargeOvercurrentThreshold2.Value - 10f;
                ChargeOvercurrentRecovery3 = ChargeOvercurrentThreshold3.Value - 10f;

                // 放电过流恢复阈值: 保护/告警/故障
                DischargeOvercurrentRecovery1 = DischargeOvercurrentThreshold1.Value - 10f;
                DischargeOvercurrentRecovery2 = DischargeOvercurrentThreshold2.Value - 10f;
                DischargeOvercurrentRecovery3 = DischargeOvercurrentThreshold3.Value - 10f;

                // 充电高温恢复阈值: 保护/告警/故障
                ChargeHighTempRecovery1 = ChargeHighTempThreshold1.Value - 3f;
                ChargeHighTempRecovery2 = ChargeHighTempThreshold2.Value - 3f;
                ChargeHighTempRecovery3 = ChargeHighTempThreshold3.Value - 3f;

                // 充电低温恢复阈值: 保护/告警/故障
                ChargeLowTempRecovery1 = ChargeLowTempThreshold1.Value + 3f;
                ChargeLowTempRecovery2 = ChargeLowTempThreshold2.Value + 3f;
                ChargeLowTempRecovery3 = ChargeLowTempThreshold3.Value + 3f;

                // 放电高温恢复阈值: 保护/告警/故障
                DischargeHighTempRecovery1 = DischargeHighTempThreshold1.Value - 3f;
                DischargeHighTempRecovery2 = DischargeHighTempThreshold2.Value - 3f;
                DischargeHighTempRecovery3 = DischargeHighTempThreshold3.Value - 3f;

                // 放电低温恢复阈值: 保护/告警/故障
                DischargeLowTempRecovery1 = DischargeLowTempThreshold1.Value + 3f;
                DischargeLowTempRecovery2 = DischargeLowTempThreshold2.Value + 3f;
                DischargeLowTempRecovery3 = DischargeLowTempThreshold3.Value + 3f;

                // SOC过低恢复阈值: 保护/告警/故障
                LowSOCRecovery1 = LowSOCTreshold1.Value + 3f;
                LowSOCRecovery2 = LowSOCTreshold2.Value + 3f;
                LowSOCRecovery3 = LowSOCTreshold3.Value + 3f;

                // 端子高温恢复阈值: 保护/告警/故障
                PoleHighTempRecovery1 = PoleHighTempThreshold1.Value - 3f;
                PoleHighTempRecovery2 = PoleHighTempThreshold2.Value - 3f;
                PoleHighTempRecovery3 = PoleHighTempThreshold3.Value - 3f;

                // 绝缘恢复阈值: 保护/告警/故障
                InsulationRecovery1 = InsulationThreshold1.Value + 10f;
                InsulationRecovery2 = InsulationThreshold2.Value + 10f;
                InsulationRecovery3 = InsulationThreshold3.Value + 10f;

                // 单体压差恢复阈值: 保护/告警/故障
                CellVoltageDifferenceRecovery1 = CellVoltageDifferenceThreshold1.Value - 0.05f;
                CellVoltageDifferenceRecovery2 = CellVoltageDifferenceThreshold2.Value - 0.05f;
                CellVoltageDifferenceRecovery3 = CellVoltageDifferenceThreshold3.Value - 0.05f;

                // 簇压差恢复阈值: 保护/告警/故障
                TotalVoltageDifferenceRecovery1 = TotalVoltageDifferenceThreshold1.Value - 2f;
                TotalVoltageDifferenceRecovery2 = TotalVoltageDifferenceThreshold2.Value - 2f;
                TotalVoltageDifferenceRecovery3 = TotalVoltageDifferenceThreshold3.Value - 2f;

                // 单体温差恢复阈值: 保护/告警/故障
                CellTempDifferenceRecovery1 = CellTempDifferenceThreshold1.Value - 1f;
                CellTempDifferenceRecovery2 = CellTempDifferenceThreshold2.Value - 1f;
                CellTempDifferenceRecovery3 = CellTempDifferenceThreshold3.Value - 1f;

                // 高压箱连接器高温恢复阈值: 保护/告警/故障
                HVBHighTempRecovery1 = HVBHighTempThreshold1.Value - 3f;
                HVBHighTempRecovery2 = HVBHighTempThreshold2.Value - 3f;
                HVBHighTempRecovery3 = HVBHighTempThreshold3.Value - 3f;
            }

            // 保护阈值
            public float? CellUndervoltageThreshold1 { get; set; } // 单体欠压保护阈值
            public float? CellOvervoltageThreshold1 { get; set; } // 单体过压保护阈值
            public float? UndervoltageThreshold1 { get; set; } // 簇欠压保护阈值
            public float? OvervoltageThreshold1 { get; set; } // 簇过压保护阈值
            public float? ChargeOvercurrentThreshold1 { get; set; } // 充电过流保护阈值
            public float? DischargeOvercurrentThreshold1 { get; set; } // 放电过流保护阈值
            public float? ChargeHighTempThreshold1 { get; set; } // 充电高温保护阈值
            public float? ChargeLowTempThreshold1 { get; set; } // 充电低温保护阈值
            public float? LowSOCTreshold1 { get; set; } // SOC过低保护阈值
            public float? PoleHighTempThreshold1 { get; set; } // 极柱温度过高保护阈值
            public float? InsulationThreshold1 { get; set; } // 绝缘保护阈值
            public float? CellVoltageDifferenceThreshold1 { get; set; } // 单体压差过高保护阈值
            public float? TotalVoltageDifferenceThreshold1 { get; set; } // 总电压压差过高保护阈值
            public float? DischargeHighTempThreshold1 { get; set; } // 放电高温保护阈值
            public float? DischargeLowTempThreshold1 { get; set; } // 放电低温保护阈值
            public float? CellTempDifferenceThreshold1 { get; set; } // 单体温差过高保护阈值
            public float? HVBHighTempThreshold1 { get; set; } // 高压箱连接器温度过高保护阈值

            // 恢复阈值
            public float? CellUndervoltageRecovery1 { get; set; } // 单体欠压保护恢复阈值
            public float? CellOvervoltageRecovery1 { get; set; } // 单体过压保护恢复阈值
            public float? UndervoltageRecovery1 { get; set; } // 簇欠压保护恢复阈值
            public float? OvervoltageRecovery1 { get; set; } // 簇过压保护恢复阈值
            public float? ChargeOvercurrentRecovery1 { get; set; } // 充电过流保护恢复阈值
            public float? DischargeOvercurrentRecovery1 { get; set; } // 放电过流保护恢复阈值
            public float? ChargeHighTempRecovery1 { get; set; } // 充电高温保护恢复阈值
            public float? ChargeLowTempRecovery1 { get; set; } // 充电低温保护恢复阈值
            public float? LowSOCRecovery1 { get; set; } // SOC过低保护恢复阈值
            public float? PoleHighTempRecovery1 { get; set; } // 极柱温度过高保护恢复阈值
            public float? InsulationRecovery1 { get; set; } // 绝缘保护恢复阈值
            public float? CellVoltageDifferenceRecovery1 { get; set; } // 单体压差过高保护恢复阈值
            public float? TotalVoltageDifferenceRecovery1 { get; set; } // 总电压压差过高保护恢复阈值
            public float? DischargeHighTempRecovery1 { get; set; } // 放电高温保护恢复阈值
            public float? DischargeLowTempRecovery1 { get; set; } // 放电低温保护恢复阈值
            public float? CellTempDifferenceRecovery1 { get; set; } // 单体温差过高保护恢复阈值
            public float? HVBHighTempRecovery1 { get; set; } // 高压箱连接器温度过高保护恢复阈值

            // 告警阈值
            public float? CellUndervoltageThreshold2 { get; set; } // 单体欠压告警阈值
            public float? CellOvervoltageThreshold2 { get; set; } // 单体过压告警阈值
            public float? UndervoltageThreshold2 { get; set; } // 簇欠压告警阈值
            public float? OvervoltageThreshold2 { get; set; } // 簇过压告警阈值
            public float? ChargeOvercurrentThreshold2 { get; set; } // 充电过流告警阈值
            public float? DischargeOvercurrentThreshold2 { get; set; } // 放电过流告警阈值
            public float? ChargeHighTempThreshold2 { get; set; } // 充电高温告警阈值
            public float? ChargeLowTempThreshold2 { get; set; } // 充电低温告警阈值
            public float? LowSOCTreshold2 { get; set; } // SOC过低告警阈值
            public float? PoleHighTempThreshold2 { get; set; } // 极柱温度过高告警阈值
            public float? InsulationThreshold2 { get; set; } // 绝缘告警阈值
            public float? CellVoltageDifferenceThreshold2 { get; set; } // 单体压差过高告警阈值
            public float? TotalVoltageDifferenceThreshold2 { get; set; } // 总电压压差过高告警阈值
            public float? DischargeHighTempThreshold2 { get; set; } // 放电高温告警阈值
            public float? DischargeLowTempThreshold2 { get; set; } // 放电低温告警阈值
            public float? CellTempDifferenceThreshold2 { get; set; } // 单体温差过高告警阈值
            public float? HVBHighTempThreshold2 { get; set; } // 高压箱连接器温度过高告警阈值

            // 恢复阈值
            public float? CellUndervoltageRecovery2 { get; set; } // 单体欠压告警恢复阈值
            public float? CellOvervoltageRecovery2 { get; set; } // 单体过压告警恢复阈值
            public float? UndervoltageRecovery2 { get; set; } // 簇欠压告警恢复阈值
            public float? OvervoltageRecovery2 { get; set; } // 簇过压告警恢复阈值
            public float? ChargeOvercurrentRecovery2 { get; set; } // 充电过流告警恢复阈值
            public float? DischargeOvercurrentRecovery2 { get; set; } // 放电过流告警恢复阈值
            public float? ChargeHighTempRecovery2 { get; set; } // 充电高温告警恢复阈值
            public float? ChargeLowTempRecovery2 { get; set; } // 充电低温告警恢复阈值
            public float? LowSOCRecovery2 { get; set; } // SOC过低告警恢复阈值
            public float? PoleHighTempRecovery2 { get; set; } // 极柱温度过高告警恢复阈值
            public float? InsulationRecovery2 { get; set; } // 绝缘告警恢复阈值
            public float? CellVoltageDifferenceRecovery2 { get; set; } // 单体压差过高告警恢复阈值
            public float? TotalVoltageDifferenceRecovery2 { get; set; } // 总电压压差过高告警恢复阈值
            public float? DischargeHighTempRecovery2 { get; set; } // 放电高温告警恢复阈值
            public float? DischargeLowTempRecovery2 { get; set; } // 放电低温告警恢复阈值
            public float? CellTempDifferenceRecovery2 { get; set; } // 单体温差过高告警恢复阈值
            public float? HVBHighTempRecovery2 { get; set; } // 高压箱连接器温度过高告警恢复阈值

            // 故障阈值
            public float? CellUndervoltageThreshold3 { get; set; } // 单体欠压故障阈值
            public float? CellOvervoltageThreshold3 { get; set; } // 单体过压故障阈值
            public float? UndervoltageThreshold3 { get; set; } // 簇欠压故障阈值
            public float? OvervoltageThreshold3 { get; set; } // 簇过压故障阈值
            public float? ChargeOvercurrentThreshold3 { get; set; } // 充电过流故障阈值
            public float? DischargeOvercurrentThreshold3 { get; set; } // 放电过流故障阈值
            public float? ChargeHighTempThreshold3 { get; set; } // 充电高温故障阈值
            public float? ChargeLowTempThreshold3 { get; set; } // 充电低温故障阈值
            public float? LowSOCTreshold3 { get; set; } // SOC过低故障阈值
            public float? PoleHighTempThreshold3 { get; set; } // 极柱温度过高故障阈值
            public float? InsulationThreshold3 { get; set; } // 绝缘故障阈值
            public float? CellVoltageDifferenceThreshold3 { get; set; } // 单体压差过高故障阈值
            public float? TotalVoltageDifferenceThreshold3 { get; set; } // 总电压压差过高故障阈值
            public float? DischargeHighTempThreshold3 { get; set; } // 放电高温故障阈值
            public float? DischargeLowTempThreshold3 { get; set; } // 放电低温故障阈值
            public float? CellTempDifferenceThreshold3 { get; set; } // 单体温差过高故障阈值
            public float? HVBHighTempThreshold3 { get; set; } // 高压箱连接器温度过高故障阈值

            // 恢复阈值
            public float? CellUndervoltageRecovery3 { get; set; } // 单体欠压故障恢复阈值
            public float? CellOvervoltageRecovery3 { get; set; } // 单体过压故障恢复阈值
            public float? UndervoltageRecovery3 { get; set; } // 簇欠压故障恢复阈值
            public float? OvervoltageRecovery3 { get; set; } // 簇过压故障恢复阈值
            public float? ChargeOvercurrentRecovery3 { get; set; } // 充电过流故障恢复阈值
            public float? DischargeOvercurrentRecovery3 { get; set; } // 放电过流故障恢复阈值
            public float? ChargeHighTempRecovery3 { get; set; } // 充电高温故障恢复阈值
            public float? ChargeLowTempRecovery3 { get; set; } // 充电低温故障恢复阈值
            public float? LowSOCRecovery3 { get; set; } // SOC过低故障恢复阈值
            public float? PoleHighTempRecovery3 { get; set; } // 极柱温度过高故障恢复阈值
            public float? InsulationRecovery3 { get; set; } // 绝缘故障恢复阈值
            public float? CellVoltageDifferenceRecovery3 { get; set; } // 单体压差过高故障恢复阈值
            public float? TotalVoltageDifferenceRecovery3 { get; set; } // 总电压压差过高故障恢复阈值
            public float? DischargeHighTempRecovery3 { get; set; } // 放电高温故障恢复阈值
            public float? DischargeLowTempRecovery3 { get; set; } // 放电低温故障恢复阈值
            public float? CellTempDifferenceRecovery3 { get; set; } // 单体温差过高故障恢复阈值
            public float? HVBHighTempRecovery3 { get; set; } // 高压箱连接器温度过高故障恢复阈值
        }

        // 电池堆数据
        public class BatteryStack
        {
            [System.Runtime.Serialization.IgnoreDataMember]
            public float NominalEnergyKWh { get; set; } = 5016f;

            [System.Runtime.Serialization.IgnoreDataMember]
            public float MaxCRate { get; set; } = 0.5f;

            public int StackId { get; set; } // 堆编号

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

        // 空调系统数据
        public class AirConditionerData
        {
            public int UnitId { get; set; } // 机组编号

            // 状态信息
            public bool? DeviceOperationStatus { get; set; } // 设备运行状态
            public bool? IndoorFanStatus { get; set; } // 室内风机状态
            public bool? OutdoorFanStatus { get; set; } // 室外风机状态
            public bool? CompressorStatus { get; set; } // 压缩机状态
            public bool? ElectricHeaterStatus { get; set; } // 电加热状态

            // 温度信息
            public float? DefrostTemp { get; set; } // 除霜温度
            public float? CondensationTemp { get; set; } // 冷凝温度
            public float? CabinetTemp { get; set; } // 机柜温度
            public float? CabinetHumidity { get; set; } // 机柜湿度

            // 设置参数
            public float? CoolingSetTemp { get; set; } // 制冷设定温度
            public float? CoolingControlHysteresis { get; set; } // 制冷回差
            public float? HeatingSetTemp { get; set; } // 制热设定温度
            public float? HeatingControlHysteresis { get; set; } // 制热回差
            public float? HumiditySetValue { get; set; } // 湿度设定值
            public float? HumidityControlHysteresis { get; set; } // 湿度回差

            // 告警信息
            public bool? CabinetOverheat { get; set; } // 机柜过热
            public bool? CabinetUnderheat { get; set; } // 机柜欠热
            public bool? HighHumidity { get; set; } // 高湿
            public bool? LowHumidity { get; set; } // 低湿
            public bool? CoilFreezeProtection { get; set; } // 盘管防冻

            // 故障信息
            public bool? DefrostSensorFault { get; set; } // 除霜传感器故障
            public bool? CondensationTempSensorFault { get; set; } // 冷凝温度传感器故障
            public bool? CabinetTempSensorFault { get; set; } // 机柜温度传感器故障
            public bool? OutletTempSensorFault { get; set; } // 出口温度传感器故障
            public bool? HumiditySensorFault { get; set; } // 湿度传感器故障
            public bool? IndoorFanFault { get; set; } // 室内风机故障
            public bool? CompressorFault { get; set; } // 压缩机故障
            public bool? HighPressureAlarm { get; set; } // 高压报警
            public bool? LowPressureAlarm { get; set; } // 低压报警
            public bool? PhaseSequenceAlarm { get; set; } // 相序报警
        }

        // 消防系统数据
        public class FireProtectionData
        {
            public int UnitId { get; set; } // 装置编号

            // 状态信息
            public bool? ExternalDetector { get; set; } // 外部探测器触发
            public bool? ExtinguishingCircuitStatus { get; set; } // 灭火回路状态
            public bool? ExtinguishingSystemFeedback { get; set; } // 灭火系统反馈
            public bool? ManualAutoStatus { get; set; } // 手动/自动状态
            public bool? DeviceStatus { get; set; } // 设备运行状态
            public bool? StartSprayStatus { get; set; } // 启动喷射状态
            public bool? GasSprayStatus { get; set; } // 气体喷射状态
            public bool? ControllerDelayStatus { get; set; } // 控制器延时状态
            public bool? BackupPowerStatus { get; set; } // 备电状态
            public bool? MainPowerStatus { get; set; } // 主电状态

            // 电池簇状态
            public Dictionary<int, bool?> BatteryClusterStatus { get; set; } = new Dictionary<int, bool?>(); // 簇编号→状态

            // 复合探测器状态
            public Dictionary<int, FireDetectorStatus> CompositeDetectorStatus { get; set; } = new Dictionary<int, FireDetectorStatus>(); // 复合探测器状态

            // 感烟感温探测器状态
            public Dictionary<int, FireDetectorStatus> SmokeDetectorStatus { get; set; } = new Dictionary<int, FireDetectorStatus>(); // 感烟探测器状态
            public Dictionary<int, FireDetectorStatus> TempDetectorStatus { get; set; } = new Dictionary<int, FireDetectorStatus>(); // 感温探测器状态

            // 中继状态
            public Dictionary<int, FireDetectorStatus> RelayStatus { get; set; } = new Dictionary<int, FireDetectorStatus>(); // 中继状态

            // 气体检测
            public float? COValue { get; set; } // CO 浓度
            public float? HydrogenValue { get; set; } // 氢气浓度
        }

        // 消防探测器状态
        public class FireDetectorStatus
        {
            public bool? FireAlarm { get; set; } // 火警
            public bool? Activation { get; set; } // 启动
            public bool? Feedback { get; set; } // 反馈
            public bool? Fault { get; set; } // 故障
        }

        // 液冷系统数据
        public class LiquidCoolingSystemData
        {
            public int UnitId { get; set; } // 机组编号

            // 运行状态
            public bool? SystemOperationStatus { get; set; } // 系统运行状态

            // 温度信息
            public float? SupplyLiquidTemp { get; set; } // 供液温度
            public float? ReturnLiquidTemp { get; set; } // 回液温度
            public float? CondensationTemp1 { get; set; } // 冷凝温度1
            public float? CondensationTemp2 { get; set; } // 冷凝温度2
            public float? EvaporationInlet1Temp { get; set; } // 蒸发器1入口温度
            public float? EvaporationOutlet1Temp { get; set; } // 蒸发器1出口温度
            public float? EvaporationInlet2Temp { get; set; } // 蒸发器2入口温度
            public float? EnvironmentTemp { get; set; } // 环境温度

            // 压力信息
            public float? SupplyLiquidPressure { get; set; } // 供液压力
            public float? ReturnLiquidPressure { get; set; } // 回液压力

            // 控制参数
            public float? ExpansionValveOpening1 { get; set; } // 电子膨胀阀1开度
            public float? ExpansionValveOpening2 { get; set; } // 电子膨胀阀2开度
            public int? OperationMode { get; set; } // 运行模式
            public int? CoolingMethod { get; set; } // 冷却方式

            // 设备状态
            public bool? WaterPump2RelayStatus { get; set; } // 水泵2继电器状态
            public bool? Heater1Output { get; set; } // 加热器1输出
            public bool? FillPumpStatus { get; set; } // 补液泵状态
            public bool? Heater2Output { get; set; } // 加热器2输出
            public bool? WaterPumpRelayOutput { get; set; } // 水泵继电器输出
            public bool? Fan1RelayOutput { get; set; } // 风机1继电器输出
            public bool? HeaterRelayOutput { get; set; } // 加热继电器输出
            public bool? Fan2RelayOutput { get; set; } // 风机2继电器输出
            public bool? AlarmRelayOutput { get; set; } // 报警继电器输出
            public bool? FillSolenoidOutput { get; set; } // 补液电磁阀输出

            // 告警和故障
            public bool? OutletOverpressureAlarm { get; set; } // 出口超压报警
            public bool? CompressorOverload { get; set; } // 压缩机过载
            public bool? WaterPumpOverload { get; set; } // 水泵过载
            public bool? HeaterFault { get; set; } // 加热器故障
            public bool? PhaseSequenceFault { get; set; } // 相序故障
            // ... 其他告警和故障字段
        }

        // 电表数据
        public class ElectricityMeterData
        {
            public int UnitId { get; set; } // 电表编号

            // 电压
            public float? PhaseAVoltage { get; set; } // A相相电压
            public float? PhaseBVoltage { get; set; } // B相相电压
            public float? PhaseCVoltage { get; set; } // C相相电压
            public float? LineABVoltage { get; set; } // AB线电压
            public float? LineBCVoltage { get; set; } // BC线电压
            public float? LineCAVoltage { get; set; } // CA线电压

            // 电流
            public float? PhaseACurrent { get; set; } // A相电流
            public float? PhaseBCurrent { get; set; } // B相电流
            public float? PhaseCCurrent { get; set; } // C相电流

            // 功率
            public float? PhaseAActivePower { get; set; } // A相有功功率
            public float? PhaseBActivePower { get; set; } // B相有功功率
            public float? PhaseCActivePower { get; set; } // C相有功功率
            public float? TotalActivePower { get; set; } // 总有功功率

            public float? PhaseAReactivePower { get; set; } // A相无功功率
            public float? PhaseBReactivePower { get; set; } // B相无功功率
            public float? PhaseCReactivePower { get; set; } // C相无功功率
            public float? TotalReactivePower { get; set; } // 总无功功率

            public float? PhaseAApparentPower { get; set; } // A相视在功率
            public float? PhaseBApparentPower { get; set; } // B相视在功率
            public float? PhaseCApparentPower { get; set; } // C相视在功率
            public float? TotalApparentPower { get; set; } // 总视在功率

            // 功率因数
            public float? PhaseAPowerFactor { get; set; } // A相功率因数
            public float? PhaseBPowerFactor { get; set; } // B相功率因数
            public float? PhaseCPowerFactor { get; set; } // C相功率因数
            public float? TotalPowerFactor { get; set; } // 总功率因数

            // 电能
            public float? ForwardActiveEnergy { get; set; } // 正向有电能
            public float? ReverseActiveEnergy { get; set; } // 反向有电能
            public float? ForwardReactiveEnergy { get; set; } // 正向无电能
            public float? ReverseReactiveEnergy { get; set; } // 反向无电能

            // 变比
            public float? PT_Ratio { get; set; } // PT变比
            public float? CT_Ratio { get; set; } // CT变比
        }

        // 温湿度传感器数据
        public class TemperatureHumidityData
        {
            public int UnitId { get; set; } // 传感器编号
            public float? Temperature { get; set; } // 温度
            public float? Humidity { get; set; } // 湿度
        }

        // IO状态数据
        public class IOStatusData
        {
            // 数字输入
            public bool? AC_Detection { get; set; }          // DI0, AC 供电检测
            public bool? EmergencyStop { get; set; }         // DI1, 急停
            public bool? BreakerCloseFeedback { get; set; }  // DI2, 断路器合位反馈
            public bool? RemoteLocalStatus { get; set; }     // DI3, 远程/本地状态
            public bool? FireMainFaultFeedback { get; set; } // DI4, 消防主故障反馈
            public bool? SingleFireAlarmFeedback { get; set; } // DI5, 单台消防报警
            public bool? FireFanStatus { get; set; }         // DI6, 消防风机状态
            public bool? DoorAccess { get; set; }            // DI7, 门禁
            public bool? WaterLeakage { get; set; }          // DI8, 漏水
            public bool? PCSAlarmSignal { get; set; }        // DI9, PCS 报警信号
            public bool? DCFuseFeedback { get; set; }        // DI10, 直流熔断器反馈
            public bool? DCSurgeProtectorFault { get; set; } // DI11, 直流防雷器故障
            public bool? ACBreakerFeedback { get; set; }     // DI12, AC 断路器反馈

            // 数字输出
            public bool? PCSDryContact { get; set; }         // DO0, PCS 干接点
            public bool? FaultIndicator { get; set; }        // DO1, 故障指示
            public bool? ElectricOperationTrip { get; set; } // DO2, 电操跳闸
            public bool? ElectricOperationClose { get; set; } // DO3, 电操合闸
            public bool? PowerDistributionTrip { get; set; }  // DO4, 配电跳闸
                                                              // ... 其他DO字段
        }

        // 辅助控制系统通信状态
        public class AuxControlCommStatus
        {
            public bool? AirConditionerCommFault { get; set; } // 空调通信故障
            public bool? FireProtectionCommFault { get; set; } // 消防通信故障
            public bool? LiquidCoolingCommFault { get; set; } // 液冷通信故障
            public bool? ElectricityMeterCommFault { get; set; } // 电表通信故障
            public bool? TempHumidityCommFault { get; set; } // 温湿度通信故障
        }

        // 完整的电池管理系统数据
        public class BatteryManagementSystemData
        {
            public DateTime Timestamp { get; set; } = DateTime.Now; // 时间戳

            // 电池堆
            public List<BatteryStack> BatteryStacks { get; set; } = new List<BatteryStack>(); // 堆列表

            // 辅助系统
            public List<AirConditionerData> AirConditioners { get; set; } = new List<AirConditionerData>(); // 空调列表
            public List<FireProtectionData> FireProtectionSystems { get; set; } = new List<FireProtectionData>(); // 消防系统列表
            public List<LiquidCoolingSystemData> LiquidCoolingSystems { get; set; } = new List<LiquidCoolingSystemData>(); // 液冷系统列表

            // 监测设备
            public List<ElectricityMeterData> ElectricityMeters { get; set; } = new List<ElectricityMeterData>(); // 电表列表
            public List<TemperatureHumidityData> TempHumiditySensors { get; set; } = new List<TemperatureHumidityData>(); // 温湿度传感器列表

            // IO状态
            public IOStatusData IOStatus { get; set; } = new IOStatusData(); // IO 状态

            // 通信状态
            public AuxControlCommStatus CommunicationStatus { get; set; } = new AuxControlCommStatus(); // 通信状态
        }
    }
   
}

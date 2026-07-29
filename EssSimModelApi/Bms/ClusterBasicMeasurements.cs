using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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
                float factor = BmsCurrentLimitDerating.ChargeLimitFactor(
                    SOC.Value, MinCellTemp, MaxCellTemp, MaxCellVoltage);
                return basePower * factor;
            }
        } 

        public float? MaxDischargePower // 最大允许放电功率
        { 
            get
            {
                if (!SOC.HasValue) return null;
                float basePower = Math.Max(0f, NominalEnergyKWh) * Math.Max(0f, MaxCRate);
                float factor = BmsCurrentLimitDerating.DischargeLimitFactor(
                    SOC.Value, MinCellTemp, MaxCellTemp, MinCellVoltage);
                return basePower * factor;
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

    }
}

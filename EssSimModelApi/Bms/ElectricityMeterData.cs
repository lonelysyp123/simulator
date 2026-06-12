using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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
}

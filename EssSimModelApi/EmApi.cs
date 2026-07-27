using System;

namespace EssSimulator.EssSimModelApi.ElectricMeter
{
    /// <summary>
    /// 电表数据模型（接口层 DTO），对应网关侧需要的电表读数。
    /// </summary>
    public class EmData
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // 电压（假定三相平衡，以线电压代填三相/线电压）
        public float PhaseAVoltage { get; set; }
        public float PhaseBVoltage { get; set; }
        public float PhaseCVoltage { get; set; }
        public float LineVoltageAB { get; set; }
        public float LineVoltageBC { get; set; }
        public float LineVoltageCA { get; set; }

        // 电流（同样假定三相平衡）
        public float PhaseACurrent { get; set; }
        public float PhaseBCurrent { get; set; }
        public float PhaseCCurrent { get; set; }

        // 功率
        public float PhaseAActivePower { get; set; }
        public float PhaseBActivePower { get; set; }
        public float PhaseCActivePower { get; set; }
        public float TotalActivePower { get; set; }
        public float PhaseAReactivePower { get; set; }
        public float PhaseBReactivePower { get; set; }
        public float PhaseCReactivePower { get; set; }
        public float TotalReactivePower { get; set; }
        public float TotalApparentPower { get; set; }
        /// <summary>功率因数：幅值 |P|/S；符号与无功同号（Q&gt;0 容性为正，与充放电无关）。</summary>
        public float PowerFactor { get; set; }
        public float Frequency { get; set; }

        // 电能（kWh / kvarh）
        public float ForwardActiveEnergy { get; set; }
        public float ReverseActiveEnergy { get; set; }
    }
}

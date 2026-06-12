using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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
}

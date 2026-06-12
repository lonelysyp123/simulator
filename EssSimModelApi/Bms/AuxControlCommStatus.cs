using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class AuxControlCommStatus
    {
        public bool? AirConditionerCommFault { get; set; } // 空调通信故障
        public bool? FireProtectionCommFault { get; set; } // 消防通信故障
        public bool? LiquidCoolingCommFault { get; set; } // 液冷通信故障
        public bool? ElectricityMeterCommFault { get; set; } // 电表通信故障
        public bool? TempHumidityCommFault { get; set; } // 温湿度通信故障
    }
}

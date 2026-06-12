using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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
}

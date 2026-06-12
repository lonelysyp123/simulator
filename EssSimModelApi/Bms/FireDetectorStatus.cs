using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class FireDetectorStatus
    {
        public bool? FireAlarm { get; set; } // 火警
        public bool? Activation { get; set; } // 启动
        public bool? Feedback { get; set; } // 反馈
        public bool? Fault { get; set; } // 故障
    }
}

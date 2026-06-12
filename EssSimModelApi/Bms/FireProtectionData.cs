using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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
}

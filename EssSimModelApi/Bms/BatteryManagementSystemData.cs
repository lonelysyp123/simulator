using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
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

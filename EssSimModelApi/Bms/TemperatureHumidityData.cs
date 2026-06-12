using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class TemperatureHumidityData
    {
        public int UnitId { get; set; } // 传感器编号
        public float? Temperature { get; set; } // 温度
        public float? Humidity { get; set; } // 湿度
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class ClusterBasicCellVoltages
    {
        // 使用字典存储单体电压，键为单体编号，值为电压值
        public Dictionary<int, float?> CellVoltages { get; set; } = new Dictionary<int, float?>(); // 单体编号→电压

        // 也可以使用数组，根据实际需要选择
         //public float?[] CellVoltageArray { get; set; } = new float?[416];
    }
}

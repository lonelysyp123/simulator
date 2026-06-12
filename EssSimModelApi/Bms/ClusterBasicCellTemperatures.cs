using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssSimModelApi.BatteryManagementSystem
{
public class ClusterBasicCellTemperatures
    {
        // 单体温度
        public Dictionary<int, float?> CellTemperatures { get; set; } = new Dictionary<int, float?>(); // 单体编号→温度

        // 极柱温度
        public Dictionary<int, float?> PositivePoleTemperatures { get; set; } = new Dictionary<int, float?>(); // 正 极柱编号→温度
        public Dictionary<int, float?> NegativePoleTemperatures { get; set; } = new Dictionary<int, float?>(); // 负 极柱编号→温度
    }
}

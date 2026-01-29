using System;
using System.Collections.Generic;
using System.Linq;

namespace IEC61850_simulatorServer2.EssDeviceSimModel
{
    // 仅用日内刻度（时:分:秒），去掉结束时间，按阶跃保持到下一刻度
    public class LoadWindow
    {
        public TimeSpan Start { get; set; }           // 起始刻度（当日时分秒）
        public double ActivePowerPlan { get; set; } = 0;     // 有功功率(kW)
        public double ReactivePowerPlan { get; set; } = 0; // 无功功率(kvar)
    }

    /// <summary>
    /// 简单的定时负载模拟器，按每日时段给出恒定有功与功率因数。
    /// </summary>
    public class ScheduledLoadSimulator
    {
        public double ActivePower { get; set; }  // 当前有功(kW)
        public double ReactivePower { get; set; }    // 当前无功(kvar)

        private List<LoadWindow> windows;
        // 灵敏系数k，
        private double k = 0.001;

        public ScheduledLoadSimulator(List<LoadWindow> window)
        {
            this.windows = window;
            SetSchedule();
        }

        // 配置每日刻度表，按 Start 升序重排
        private void SetSchedule()
        {
            windows = windows.OrderBy(w => w.Start).ToList();
        }

        // 根据仿真时间（取其当天时刻）获取当前负载功率与功率因数：
        // 找到最后一个 Start <= 当前时刻的窗口，若无则取末条（前一天延续）
        private void UpdateCurrentLoad(DateTime simTime)
        {
            if (isStoppedLoadWindows)
            {
                return;
            }
            var tod = simTime.TimeOfDay;
            // 默认使用首条，遍历找到最后一个 Start<=当前时刻
            LoadWindow active = windows[0];

            for (int i = 0; i < windows.Count; i++)
            {
                if (tod >= windows[i].Start)
                {
                    active = windows[i];
                }
                else
                {
                    break; // 第一个 Start 大于当前时刻，停
                }
            }
            ActivePower = active.ActivePowerPlan;
            ReactivePower = active.ReactivePowerPlan;
        }

        // 将有功功率换算为交流电流(A)，假设单一功率因数
        public double ComputeLoadCurrentA(ref double voltage)
        {
            UpdateCurrentLoad(DateTime.Now);

            if (ReactivePower > 0)
            {
                voltage = voltage - ReactivePower * k;
            }

            if (voltage <= 0) return 0;
            var p = ActivePower;
            var q = ReactivePower;
            var sKva = Math.Sqrt(p * p + q * q); // 视在功率(kVA)
            if (sKva <= 0) return 0;

            return sKva * 1000.0 / voltage; // I = S/V，默认单相或等效相电压
        }

        private bool isStoppedLoadWindows = false;
        public void SetLoadCharacteristic(string characteristic, double value)
        {
            isStoppedLoadWindows = true;
            if (characteristic == "activePower")
            {
                ActivePower = value;
            }
            else if (characteristic == "reactivePower")
            {
                ReactivePower = value;
            }
        }
    }
}

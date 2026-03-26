using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.EssDeviceSimModel
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
        public double ReactivePower { get; set; }    // 当前无功(kvar, legacy符号: 正=感性吸收)

        private List<LoadWindow> windows;
        // 灵敏系数k，
        private readonly double k;

        public ScheduledLoadSimulator(List<LoadWindow> window, double reactiveVoltageFeedbackCoefficient = 0.001)
        {
            this.windows = window;
            k = reactiveVoltageFeedbackCoefficient;
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

        // 将有功功率换算为交流电流(A)。
        // 统一测点：入参 voltage 为并网点电压（变压器二次侧线电压）。
        public double ComputeLoadCurrentA(ref double voltage)
        {
            UpdateCurrentLoad(DateTime.Now);
            // 按统一约定封装 legacy 反馈：现阶段保持原行为不变，仅标准化入口。
            voltage = GridFeedbackConventions.ApplyLegacyLoadVoltageFeedback(voltage, ReactivePower, k);

            if (voltage <= 0) return 0;
            var p = ActivePower;
            var q = ReactivePower;
            var sKva = Math.Sqrt(p * p + q * q); // 视在功率(kVA)
            if (sKva <= 0) return 0;

            // 与 PCS 三相口径保持一致：I = S / (sqrt(3) * Uline)
            return sKva * 1000.0 / (voltage * Math.Sqrt(3.0));
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

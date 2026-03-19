using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EssSimulator.EssDeviceSimModel
{
    public class LiFePO4BatteryOCVModel
    {
        // 开路OCV-SOC曲线数据
        private static readonly Dictionary<double, double> OpenCircuitOcvCurve = new Dictionary<double, double>
    {
        {0.00, 3.050}, {0.05, 3.150}, {0.10, 3.200}, {0.15, 3.210},
        {0.20, 3.220}, {0.25, 3.225}, {0.30, 3.230}, {0.35, 3.235},
        {0.40, 3.240}, {0.45, 3.245}, {0.50, 3.250}, {0.55, 3.255},
        {0.60, 3.260}, {0.65, 3.265}, {0.70, 3.270}, {0.75, 3.275},
        {0.80, 3.280}, {0.85, 3.285}, {0.90, 3.295}, {0.95, 3.310},
        {1.00, 3.330}
    };

        // 已知倍率下的最大电压偏移量 (SOC -> 最大偏移量)
        private static readonly Dictionary<double, double> MaxChargeVoltageOffsets = new Dictionary<double, double>
    {
        {0.00, 0.00}, {0.20, 0.15}, {0.40, 0.22}, {0.60, 0.26}, {0.80, 0.35}, {1.00, 0.45}
    };

        private static readonly Dictionary<double, double> MaxDischargeVoltageOffsets = new Dictionary<double, double>
    {
        {0.00, 0.00}, {0.20, -0.15}, {0.40, -0.22}, {0.60, -0.26}, {0.80, -0.35}, {1.00, -0.45}
    };

        // 参考倍率 (2C)
        private const double ReferenceCrate = 2.0;

        /// <summary>
        /// 根据SOC获取电压 (支持任意充放电率)
        /// </summary>
        /// <param name="soc">荷电状态 (0.0-1.0)</param>
        /// <param name="cRate">充放电率 (正值，单位C)</param>
        /// <param name="isCharging">是否在充电状态</param>
        /// <returns>电压值 (V)</returns>
        public static double GetVoltageFromSOC(double soc, double cRate, bool isCharging)
        {
            // 限制SOC范围
            soc = Math.Max(0.0, Math.Min(1.0, soc));

            // 开路情况直接使用OCV曲线
            if (cRate <= 0.0)
            {
                return GetInterpolatedValue(OpenCircuitOcvCurve, soc);
            }

            // 获取开路电压
            double ocv = GetInterpolatedValue(OpenCircuitOcvCurve, soc);

            // 获取电压偏移量
            double offset = GetDynamicVoltageOffset(soc, cRate, isCharging);

            return ocv + offset;
        }

        /// <summary>
        /// 根据电压获取SOC (支持任意充放电率)
        /// </summary>
        /// <param name="voltage">电压值 (V)</param>
        /// <param name="cRate">充放电率 (正值，单位C)</param>
        /// <param name="isCharging">是否在充电状态</param>
        /// <returns>荷电状态 (0.0-1.0)</returns>
        public static double GetSOCFromVoltage(double voltage, double cRate, bool isCharging)
        {
            // 开路情况直接使用OCV曲线
            if (cRate <= 0.0)
            {
                return GetInterpolatedKey(OpenCircuitOcvCurve, voltage);
            }

            // 使用迭代法反向计算SOC
            double tolerance = 0.001; // 容差
            double lowerSoc = 0.0;
            double upperSoc = 1.0;
            double currentSoc = 0.5;
            int maxIterations = 100;
            int iteration = 0;

            while (iteration++ < maxIterations)
            {
                double currentVoltage = GetVoltageFromSOC(currentSoc, cRate, isCharging);

                if (Math.Abs(currentVoltage - voltage) < tolerance)
                {
                    return currentSoc;
                }

                if (currentVoltage < voltage)
                {
                    lowerSoc = currentSoc;
                }
                else
                {
                    upperSoc = currentSoc;
                }

                currentSoc = (lowerSoc + upperSoc) / 2;
            }

            return currentSoc;
        }

        // 动态计算电压偏移量
        private static double GetDynamicVoltageOffset(double soc, double cRate, bool isCharging)
        {
            // 获取最大偏移量曲线
            var maxOffsetCurve = isCharging ? MaxChargeVoltageOffsets : MaxDischargeVoltageOffsets;

            // 插值获取当前SOC点的最大偏移量
            double maxOffset = GetInterpolatedValue(maxOffsetCurve, soc);

            // 计算偏移比例 (非线性关系，使用平方根函数近似)
            double rateRatio = Math.Min(cRate / ReferenceCrate, 1.0);
            double offsetRatio = Math.Sqrt(rateRatio); // 使用平方根模拟非线性关系

            return maxOffset * offsetRatio;
        }

        // 辅助方法：从字典中获取插值后的值
        private static double GetInterpolatedValue(Dictionary<double, double> dict, double key)
        {
            var lower = dict.Keys.Where(k => k <= key).DefaultIfEmpty(0.0).Max();
            var upper = dict.Keys.Where(k => k >= key).DefaultIfEmpty(1.0).Min();

            if (lower == upper) return dict[lower];

            var ratio = (key - lower) / (upper - lower);
            return dict[lower] + ratio * (dict[upper] - dict[lower]);
        }

        // 辅助方法：从字典中获取插值后的键
        private static double GetInterpolatedKey(Dictionary<double, double> dict, double value)
        {
            if (value <= dict[0.0]) return 0.0;
            if (value >= dict[1.0]) return 1.0;

            var lowerKv = dict.Where(kv => kv.Value <= value)
                             .OrderByDescending(kv => kv.Key)
                             .FirstOrDefault();
            var upperKv = dict.Where(kv => kv.Value >= value)
                             .OrderBy(kv => kv.Key)
                             .FirstOrDefault();

            if (Math.Abs(lowerKv.Value - upperKv.Value) < 0.001)
                return lowerKv.Key;

            var ratio = (value - lowerKv.Value) / (upperKv.Value - lowerKv.Value);
            return lowerKv.Key + ratio * (upperKv.Key - lowerKv.Key);
        }

        // 示例用法
        //public static void Main(string[] args)
        //{
        //    // 示例1: 不同充放电率下的SOC转电压
        //    double soc = 0.6;
        //    Console.WriteLine($"SOC {soc * 100}% 在不同充放电率下的电压:");

        //    double[] rates = { 0.0, 0.1, 0.5, 1.0, 2.0, 3.0, 4.0 };
        //    foreach (double rate in rates)
        //    {
        //        double chargeVoltage = GetVoltageFromSOC(soc, rate, true);
        //        double dischargeVoltage = GetVoltageFromSOC(soc, rate, false);

        //        Console.WriteLine($"{rate:F1}C: 充电={chargeVoltage:F3}V, 放电={dischargeVoltage:F3}V");
        //    }
        //    Console.WriteLine();

        //    // 示例2: 不同充放电率下的电压转SOC
        //    double voltage = 3.30;
        //    Console.WriteLine($"电压 {voltage:F2}V 在不同充放电率下的SOC:");

        //    foreach (double rate in rates)
        //    {
        //        double chargeSOC = GetSOCFromVoltage(voltage, rate, true);
        //        double dischargeSOC = GetSOCFromVoltage(voltage, rate, false);

        //        Console.WriteLine($"{rate:F1}C: 充电={chargeSOC * 100:F1}%, 放电={dischargeSOC * 100:F1}%");
        //    }
        //}
    }
}

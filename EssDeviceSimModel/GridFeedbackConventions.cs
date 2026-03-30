using System;

namespace EssSimulator.EssDeviceSimModel
{
    /// <summary>
    /// 无功-电压反馈统一约定（符号与测点定义）。
    /// 说明：
    /// 1) 并网点无功符号约定：Q > 0 表示升压支撑，Q < 0 表示降压作用。
    /// 2) 并网测点统一定义为变压器二次侧线电压（并网点/母线电压）。
    /// </summary>
    public static class GridFeedbackConventions
    {
        /// <summary>
        /// 并网测点名称（用于日志、注释与后续配置映射）。
        /// </summary>
        public const string PccVoltageMeasurementPoint = "Transformer.SecondaryVoltage";

        /// <summary>
        /// 计算并网点无功引起的电压偏移（pu）。
        /// 方向约定：
        /// - Q > 0 => 正偏移量，表示电压抬升；
        /// - Q < 0 => 负偏移量，表示电压下拉。
        /// </summary>
        /// <param name="legacyReactiveKvar">并网点总无功（kvar）</param>
        /// <param name="ratedPowerKva">额定容量（kVA）</param>
        /// <param name="impedancePu">等效阻抗（pu）</param>
        /// <param name="influenceCoefficient">无功电压影响系数（无量纲）</param>
        public static double CalculatePccReactiveVoltageShiftPu(
            double legacyReactiveKvar,
            double ratedPowerKva,
            double impedancePu,
            double influenceCoefficient)
        {
            if (ratedPowerKva <= 0) return 0;
            return influenceCoefficient * impedancePu * (legacyReactiveKvar / ratedPowerKva);
        }

        /// <summary>
        /// 在当前 legacy 负载模型下应用电压反馈。
        /// 现行为：
        /// - Q > 0 时按 k 上抬电压；
        /// - Q < 0 时按 k 下拉电压；
        /// - Qlegacy = 0 时电压不变。
        /// </summary>
        public static double ApplyLegacyLoadVoltageFeedback(double inputVoltage, double legacyReactiveKvar, double k)
        {
            if (legacyReactiveKvar > 0)
            {
                return inputVoltage + legacyReactiveKvar * k;
            }
            if (legacyReactiveKvar < 0)
            {
                return inputVoltage + legacyReactiveKvar * k;
            }
            return inputVoltage;
        }
    }
}

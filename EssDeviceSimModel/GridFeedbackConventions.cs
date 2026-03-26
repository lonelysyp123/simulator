using System;

namespace EssSimulator.EssDeviceSimModel
{
    /// <summary>
    /// 无功-电压反馈统一约定（符号与测点定义）。
    /// 说明：
    /// 1) legacy 无功符号沿用现网逻辑：Qlegacy > 0 表示感性吸收（通常拉低电压）。
    /// 2) 并网测点统一定义为变压器二次侧线电压（并网点/母线电压）。
    /// </summary>
    public static class GridFeedbackConventions
    {
        /// <summary>
        /// 并网测点名称（用于日志、注释与后续配置映射）。
        /// </summary>
        public const string PccVoltageMeasurementPoint = "Transformer.SecondaryVoltage";

        /// <summary>
        /// 在当前 legacy 负载模型下应用电压反馈。
        /// 现行为：
        /// - Qlegacy > 0 时按 k 下拉电压；
        /// - Qlegacy < 0 时按 k 上抬电压；
        /// - Qlegacy = 0 时电压不变。
        /// </summary>
        public static double ApplyLegacyLoadVoltageFeedback(double inputVoltage, double legacyReactiveKvar, double k)
        {
            if (legacyReactiveKvar > 0)
            {
                return inputVoltage - legacyReactiveKvar * k;
            }
            if (legacyReactiveKvar < 0)
            {
                return inputVoltage - legacyReactiveKvar * k;
            }
            return inputVoltage;
        }
    }
}

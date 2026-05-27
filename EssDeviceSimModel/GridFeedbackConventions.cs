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
        /// 并网测点名称（用于日志、注释与后续配置映射）：220kV PCC / 并网电表电压。
        /// </summary>
        public const string PccVoltageMeasurementPoint = "EnergyStorageSystem.PccLineVoltageV";

        /// <summary>
        /// 由并网点总无功计算 220kV PCC 线电压（V）。
        /// ΔU_pu = k × (Q_kvar / (S_sc_MVA × 1000))，并限幅。
        /// </summary>
        public static double CalculatePccLineVoltage(
            double nominalLineVoltageV,
            double totalReactiveKvar,
            double shortCircuitMva,
            double influenceCoefficient,
            double maxVoltageShiftPercent)
        {
            if (nominalLineVoltageV <= 0) return 0;
            if (shortCircuitMva <= 0) return nominalLineVoltageV;

            double shortCircuitKva = shortCircuitMva * 1000.0;
            double shiftPu = influenceCoefficient * (totalReactiveKvar / shortCircuitKva);
            double maxShiftPu = Math.Max(0, maxVoltageShiftPercent) / 100.0;
            shiftPu = Math.Clamp(shiftPu, -maxShiftPu, maxShiftPu);
            return nominalLineVoltageV * (1.0 + shiftPu);
        }

        /// <summary>
        /// 由 220kV PCC 线电压按主变额定变比推导 35kV 站内母线电压（V）。
        /// </summary>
        public static double DeriveStationBusVoltage(
            double pccLineVoltageV,
            double pccNominalLineVoltageV,
            double stationBusNominalLineVoltageV)
        {
            if (pccLineVoltageV <= 0 || pccNominalLineVoltageV <= 0) return 0;
            return pccLineVoltageV * (stationBusNominalLineVoltageV / pccNominalLineVoltageV);
        }

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

using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.ElectricMeter;

namespace EssSimulator.EssSimModelApi.Mappers
{
    /// <summary>
    /// 将 ESS 物理模型数据映射到电表接口 DTO（EmData）。
    /// </summary>
    public static class EmMapper
    {
        /// <summary>
        /// 更新电表瞬时量和电能累积量。
        /// </summary>
        /// <param name="ess">ESS 物理模型</param>
        /// <param name="emData">目标 EmData DTO</param>
        /// <param name="dt">自上次调用的时间差（用于电能积分）</param>
        /// <param name="forwardKWh">正向电能累积（由调用方维护）</param>
        /// <param name="reverseKWh">反向电能累积（由调用方维护）</param>
        public static void MapEssToEmData(EnergyStorageSystem ess, EmData emData, TimeSpan dt,
            ref double forwardKWh, ref double reverseKWh)
        {
            var xfSt = ess._transformer.GetCurrentState();

            // 统一测点：并网点线电压（PCC）取变压器二次侧电压。
            double lineVoltage = xfSt?.SecondaryVoltage > 0
                ? xfSt.SecondaryVoltage
                : ess._transformer._specs.SecondaryVoltage;

            // 并网点功率约定：向电网送出为正，负载消耗视为负注入。
            double loadP = -ess._loadSimulator.ActivePower;
            // 无功沿用 legacy 符号，并在并网点统计时按负载消耗取负注入。
            double loadQ = -ess._loadSimulator.ReactivePower;

            double totalP = loadP, totalQ = loadQ;
            foreach (var pcs in ess._pcsList)
            {
                var st = pcs.GetCurrentState();
                totalP += pcs.GetGridSideActivePower();
                totalQ += st.ReactivePower;
            }
            double totalS = Math.Sqrt(totalP * totalP + totalQ * totalQ);
            double pf     = totalS > 0 ? Math.Clamp(totalP / totalS, -1.0, 1.0) : 1.0;
            double I      = lineVoltage > 0 ? totalS * 1000.0 / lineVoltage / Math.Sqrt(3.0) : 0.0;

            double phaseV = lineVoltage / Math.Sqrt(3.0);

            emData.PhaseAVoltage  = emData.PhaseBVoltage  = emData.PhaseCVoltage  = (float)phaseV;
            emData.LineVoltageAB  = emData.LineVoltageBC  = emData.LineVoltageCA  = (float)lineVoltage;
            emData.PhaseACurrent  = emData.PhaseBCurrent  = emData.PhaseCCurrent  = (float)I;

            double perP = totalP / 3.0;
            double perQ = totalQ / 3.0;
            emData.PhaseAActivePower   = emData.PhaseBActivePower   = emData.PhaseCActivePower   = (float)perP;
            emData.PhaseAReactivePower = emData.PhaseBReactivePower = emData.PhaseCReactivePower = (float)perQ;
            emData.TotalActivePower    = (float)totalP;
            emData.TotalReactivePower  = (float)totalQ;
            emData.TotalApparentPower  = (float)totalS;
            emData.PowerFactor         = (float)pf;
            emData.Frequency           = 50.0f;

            double hours = dt.TotalHours;
            if (totalP >= 0) forwardKWh += totalP * hours;
            else             reverseKWh -= totalP * hours;

            emData.ForwardActiveEnergy = (float)forwardKWh;
            emData.ReverseActiveEnergy = (float)reverseKWh;
        }
    }
}

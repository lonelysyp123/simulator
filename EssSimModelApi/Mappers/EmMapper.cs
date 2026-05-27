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
            var xfSt = ess._mainTransformer.GetCurrentState();

            // 主变端子检测电表：优先读取主变一次侧端子电压。
            // 主断分闸时，若35kV侧被黑启动反送，主变一次侧可出现感应电压，电表应可见该电压。
            double lineVoltage = xfSt != null
                ? Math.Max(0.0, xfSt.PrimaryVoltage)
                : (ess.PccLineVoltageV > 0 ? ess.PccLineVoltageV : 0.0);

            // 并网点功率方向约定：+ 向电网送电（放电），- 从电网取电（用电）。
            double loadP = ess._loadSimulator.ActivePower;
            // 无功沿用 legacy 符号，按同方向约定直接汇总。
            double loadQ = ess._loadSimulator.ReactivePower;

            double totalP = loadP, totalQ = loadQ;
            foreach (var pcs in ess._pcsList)
            {
                var st = pcs.GetCurrentState();
                totalP += pcs.GetGridSideActivePower();
                totalQ += st.ReactivePower;
            }
            double totalS = Math.Sqrt(totalP * totalP + totalQ * totalQ);
            double pf     = totalS > 0 ? Math.Clamp(totalP / totalS, -1.0, 1.0) : 1.0;

            // 主变端子检测口径下，主断分闸时端子交换功率应约为0（无外部电源通道），
            // 保持 P/Q/S 归零可避免将站内35kV侧无功误记到220kV断开侧。
            if (!ess._breaker.IsClosed)
            {
                totalP = 0;
                totalQ = 0;
                totalS = 0;
                pf = 1.0;
            }

            // 一次侧电流优先采用主变模型端子电流；仅在并网闭合且端子电流缺失时用 S/U 回退估算。
            double I = xfSt != null ? xfSt.PrimaryCurrent : 0.0;
            if (Math.Abs(I) < 1e-9 && ess._breaker.IsClosed)
            {
                I = lineVoltage > 0 ? totalS * 1000.0 / lineVoltage / Math.Sqrt(3.0) : 0.0;
            }

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

using EssSimulator.EssDeviceSimModel.Devices;

namespace EssSimulator.EssDeviceSimModel
{
    /// <summary>离网/黑启动场景下的母线电压估算。</summary>
    public static class EssIslandBusLogic
    {
        /// <summary>PCS 是否处于离网 V/f 建压（黑启动或孤岛电压有效）。</summary>
        public static bool IsPcsIslandVoltageBuilding(PcsState st)
        {
            if (st.Mode != OperationMode.Normal || st.GMode != GridMode.Islanded)
                return false;
            if (st.BlackStartEnabled)
                return st.BlackStartPhase is BlackStartPhase.SoftStarting
                    or BlackStartPhase.VoltageRegulating
                    or BlackStartPhase.Synchronized;
            return st.IslandVoltageEffectiveV > 1.0;
        }

        /// <summary>
        /// 在主断分闸时估算离网 35kV 母线电压：取所有“单元高压合 + 正在离网建压”PCS 的最高二次侧电压，
        /// 再按单元变变比折算到 35kV 侧。
        /// </summary>
        public static double EstimateIslandedBus35LineVoltageV(
            IReadOnlyList<TransformerDevice> unitTransformers,
            IReadOnlyList<Breaker> unitBreakers,
            IReadOnlyList<PcsDevice> pcsList,
            IReadOnlyList<int>? pcsPerUnit = null)
        {
            double bus35 = 0;
            for (int u = 0; u < unitTransformers.Count; u++)
            {
                if (u >= unitBreakers.Count || !unitBreakers[u].IsClosed)
                    continue;

                var (baseIdx, pcsCount) = PcsUnitLayout.RangeOfUnit(pcsPerUnit, u);
                double lv690 = 0;

                void Acc(int idx)
                {
                    if (idx < 0 || idx >= pcsList.Count) return;
                    var st = pcsList[idx].GetCurrentState();
                    if (!IsPcsIslandVoltageBuilding(st)) return;
                    lv690 = Math.Max(lv690, st.AcVoltage);
                }

                for (int ch = 0; ch < pcsCount; ch++)
                    Acc(baseIdx + ch);
                if (lv690 <= 0)
                    continue;

                bus35 = Math.Max(bus35, lv690 * unitTransformers[u].TurnsRatio);
            }
            return bus35;
        }
    }
}

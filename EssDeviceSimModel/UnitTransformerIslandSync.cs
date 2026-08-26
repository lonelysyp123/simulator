using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Devices;

namespace EssSimulator.EssDeviceSimModel
{
    /// <summary>
    /// 离网/黑启动场景下单元变与 35kV 母线耦合，以及站用电在在线 PCS 间的分摊。
    /// 在 PCS.Update 之后调用，使变压器状态与当步 PCS 输出同 tick 对齐。
    /// </summary>
    public static class UnitTransformerIslandSync
    {
        public static void SyncAfterPcsUpdate(
            bool mainBreakerClosed,
            double stationBus35LineVoltageV,
            IReadOnlyList<TransformerDevice> unitTransformers,
            TransformerDevice mainTransformer,
            IReadOnlyList<PcsDevice> pcsList,
            Func<int, bool> isUnitBreakerClosed,
            PcsPhysicalConfig pcsCfg,
            IReadOnlyList<int>? pcsPerUnit,
            DateTime simTime,
            TimeSpan simStep)
        {
            int unitCount = unitTransformers.Count;
            if (unitCount == 0)
                return;

            var unitHvClosed = new bool[unitCount];
            var localUnitP = new double[unitCount];
            var localUnitQ = new double[unitCount];
            var localLv690 = new double[unitCount];
            var unitPrimaryV = new double[unitCount];

            double sharedBus35kVFromIsland = 0;

            for (int u = 0; u < unitCount; u++)
            {
                var (baseIdx, pcsCount) = PcsUnitLayout.RangeOfUnit(pcsPerUnit, u);
                unitHvClosed[u] = isUnitBreakerClosed(u);
                if (!unitHvClosed[u])
                    continue;

                void Accumulate(int pcsIdx)
                {
                    if (pcsIdx < 0 || pcsIdx >= pcsList.Count) return;
                    var st = pcsList[pcsIdx].GetCurrentState();
                    if (!EssIslandBusLogic.IsPcsIslandVoltageBuilding(st)) return;
                    localLv690[u] = Math.Max(localLv690[u], st.AcVoltage);
                    localUnitP[u] += pcsList[pcsIdx].GetGridSideActivePower();
                    localUnitQ[u] += st.ReactivePower;
                }

                for (int ch = 0; ch < pcsCount; ch++)
                    Accumulate(baseIdx + ch);

                if (mainBreakerClosed || localLv690[u] <= 0)
                    continue;

                sharedBus35kVFromIsland = Math.Max(
                    sharedBus35kVFromIsland, localLv690[u] * unitTransformers[u].TurnsRatio);
            }

            for (int u = 0; u < unitCount; u++)
            {
                if (!unitHvClosed[u])
                {
                    unitTransformers[u].Update(0, 0, 1.0, 0, 0, simTime, simStep);
                    continue;
                }

                double turnsRatio = unitTransformers[u].TurnsRatio;

                unitPrimaryV[u] = mainBreakerClosed
                    ? stationBus35LineVoltageV
                    : sharedBus35kVFromIsland;

                if (unitPrimaryV[u] <= 0)
                {
                    unitTransformers[u].Update(0, 0, 1.0, 0, 0, simTime, simStep);
                    continue;
                }

                double secV = Math.Max(unitPrimaryV[u] / Math.Max(turnsRatio, 1e-6), 1.0);
                double unitS = Math.Sqrt(localUnitP[u] * localUnitP[u] + localUnitQ[u] * localUnitQ[u]);
                double unitPf = unitS > 0 ? localUnitP[u] / unitS : 1.0;
                double unitSecCurrentMag = unitS * 1000.0 / (secV * Math.Sqrt(3.0));
                double unitSecCurrent = Math.Abs(localUnitP[u]) > 1e-6
                    ? (localUnitP[u] >= 0 ? -unitSecCurrentMag : unitSecCurrentMag)
                    : unitSecCurrentMag;

                unitTransformers[u].Update(
                    unitPrimaryV[u], unitSecCurrent, unitPf, unitS, localUnitQ[u], simTime, simStep,
                    applyReactiveVoltageShift: false);
            }

            ApplyBlackStartStationElectricalLoadAcrossBus(
                unitTransformers, mainTransformer, pcsList, pcsCfg,
                localUnitP, unitPrimaryV, mainBreakerClosed, stationBus35LineVoltageV, pcsPerUnit);
        }

        private static void ApplyBlackStartStationElectricalLoadAcrossBus(
            IReadOnlyList<TransformerDevice> unitTransformers,
            TransformerDevice mainTransformer,
            IReadOnlyList<PcsDevice> pcsList,
            PcsPhysicalConfig pcsCfg,
            double[] localUnitP,
            double[] unitPrimaryV,
            bool mainBreakerClosed,
            double stationBus35LineVoltageV,
            IReadOnlyList<int>? pcsPerUnit)
        {
            double totalMagQ = 0;
            double totalLossP = 0;
            double lineCoeff = Math.Clamp(pcsCfg.GridLossCoefficient, 0, 0.5);

            for (int u = 0; u < unitTransformers.Count; u++)
            {
                if (u >= unitPrimaryV.Length || unitPrimaryV[u] <= 0)
                    continue;
                var xf = unitTransformers[u];
                totalMagQ += xf.GetSecondaryMagnetizingReactiveKvar();
                double ironKw = xf.GetSecondaryNoLoadActivePowerKw();
                double lineKw = Math.Abs(localUnitP[u]) * lineCoeff / Math.Max(1e-6, 1.0 - lineCoeff);
                totalLossP += ironKw + lineKw;
            }

            if (!mainBreakerClosed && stationBus35LineVoltageV > 1.0)
                totalMagQ += mainTransformer.GetSecondaryMagnetizingReactiveKvar();

            var participants = new List<int>();
            for (int i = 0; i < pcsList.Count; i++)
            {
                var st = pcsList[i].GetCurrentState();
                if (st.BlackStartEnabled && EssIslandBusLogic.IsPcsIslandVoltageBuilding(st))
                    participants.Add(i);
            }

            for (int i = 0; i < pcsList.Count; i++)
            {
                pcsList[i].SetTransformerMagnetizingReactiveKvar(0);
                pcsList[i].SetBlackStartSharedLossActivePowerKw(0);
                pcsList[i].SetBlackStartInrushDemand(0, 0);
            }

            if (participants.Count == 0)
                return;

            ApplyUnitTransformerInrushDemand(unitTransformers, unitPrimaryV, pcsList, participants, pcsPerUnit);

            double qEach = totalMagQ / participants.Count;
            double pEach = totalLossP / participants.Count;
            foreach (var idx in participants)
            {
                pcsList[idx].SetTransformerMagnetizingReactiveKvar(qEach);
                pcsList[idx].SetBlackStartSharedLossActivePowerKw(pEach);
            }
        }

        private static void ApplyUnitTransformerInrushDemand(
            IReadOnlyList<TransformerDevice> unitTransformers,
            double[] unitPrimaryV,
            IReadOnlyList<PcsDevice> pcsList,
            List<int> participants,
            IReadOnlyList<int>? pcsPerUnit)
        {
            var participantSet = participants.ToHashSet();
            for (int u = 0; u < unitTransformers.Count; u++)
            {
                if (u >= unitPrimaryV.Length || unitPrimaryV[u] <= 0)
                    continue;

                var (pInrush, qInrush) = unitTransformers[u].GetInrushDemandKwKvar();
                if (pInrush <= 1e-6 && qInrush <= 1e-6)
                    continue;

                var unitPcs = new List<int>();
                var (baseIdx, pcsCount) = PcsUnitLayout.RangeOfUnit(pcsPerUnit, u);
                for (int ch = 0; ch < pcsCount; ch++)
                {
                    int idx = baseIdx + ch;
                    if (idx >= 0 && idx < pcsList.Count && participantSet.Contains(idx))
                        unitPcs.Add(idx);
                }
                if (unitPcs.Count == 0)
                    continue;

                double pEach = pInrush / unitPcs.Count;
                double qEach = qInrush / unitPcs.Count;
                foreach (int idx in unitPcs)
                    pcsList[idx].SetBlackStartInrushDemand(pEach, qEach);
            }
        }
    }
}

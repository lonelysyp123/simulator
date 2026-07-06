using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Solver;
using log4net;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 径向网络前推回代求解：
    /// ① 叶子汇报 P/Q → ② 自下而上汇总 → ③ 电网 Q-U 定压 → ④ Coupler 链传播电压 → ⑤ 算电流并 Step 设备
    /// → ⑥ 按实测 Q 多轮 Q-U/V 反馈迭代直至收敛。
    /// </summary>
    public sealed class RadialPowerSweepEngine
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(RadialPowerSweepEngine));
        private readonly RadialNetworkGraph _graph;
        private readonly ElectricalNetwork _network;
        private readonly EnergyStorageSystem _ess;
        private readonly PccConfig _pccCfg;
        private readonly PcsPhysicalConfig _pcsCfg;
        private readonly ISelfActivatingElectricalSource _grid;
        private readonly int _quvMaxIterations;
        private readonly double _voltageTolerancePu;
        private double _lastBus35LineVoltageV;

        public RadialPowerSweepEngine(
            RadialNetworkGraph graph,
            EnergyStorageSystem ess,
            PccConfig pccCfg,
            PcsPhysicalConfig pcsCfg,
            int propagationQuvMaxIterations = 3,
            double propagationVoltageTolerancePu = 0.001)
        {
            _graph = graph;
            _network = graph.Network;
            _ess = ess;
            _pccCfg = pccCfg;
            _pcsCfg = pcsCfg;
            _quvMaxIterations = Math.Max(1, propagationQuvMaxIterations);
            _voltageTolerancePu = Math.Max(0, propagationVoltageTolerancePu);
            _grid = _network.Grid as ISelfActivatingElectricalSource
                ?? throw new InvalidOperationException("Grid 必须实现 ISelfActivatingElectricalSource");
            _lastBus35LineVoltageV = pccCfg.StationBusNominalLineVoltage;
            _log.Info(
                $"[RadialSweep] 母线前推回代引擎已就绪（Q-U/V 最多 {_quvMaxIterations} 轮，容差 {_voltageTolerancePu:P3} pu）");
        }

        public RadialNetworkGraph Graph => _graph;

        /// <summary>电压源发起一次完整求解周期（默认 100ms 调用）。</summary>
        /// <param name="step">潮流/Coupler 步长。</param>
        /// <param name="meterIntegrationStep">电表电能积分步长（上次回调至本次的真实间隔 × IntegrationStepMultiplier）。</param>
        public void SolveCycle(DateTime simTime, TimeSpan step, TimeSpan meterIntegrationStep)
        {
            var context = BuildContext(simTime, step);
            NetworkControlBridge.SyncLoadPlan(_network, _ess._loadDevice, simTime);
            NetworkControlBridge.SyncBmsLinksFromRacks(_network, _ess._bmsRackDevices);

            Phase1CollectLeafPower(context);
            Phase2AggregatePowerBottomUp(context);
            Phase3GridVoltageSolve(context, step);
            Phase4PropagateVoltageTopDown(context, step);
            Phase5AssignCurrentsAndStepDevices(context, step);
            RefreshSeriesDevicesAfterLeafStep(context, step);

            RunQuvRefinementIterations(context, step);
            SystemFrequencyResolver.Refresh(_network, context);
            SamplePccMeter(context, meterIntegrationStep, _graph.Bus35.LineVoltageV);

            NetworkStepOrchestrator.ApplyGridResultsToEnergyStorageSystem(
                _network, _ess, simTime, step, _pcsCfg);
            NetworkControlBridge.ProjectBreakersToLegacy(_network, _ess);
            PublishBusQuantities();
        }

        /// <summary>① 叶子设备上报 P/Q 意图（不依赖电压源指定电流）。</summary>
        private void Phase1CollectLeafPower(DeviceStepContext context)
        {
            _graph.Bus35.ResetPowerAggregation();
            foreach (var bus690 in _graph.UnitBuses690)
                bus690.ResetPowerAggregation();
        }

        /// <summary>② 690V 汇总 → 35kV 全站 P/Q。</summary>
        private void Phase2AggregatePowerBottomUp(DeviceStepContext context)
        {
            _graph.Bus35.CollectFromContributors(context);

            foreach (var bus690 in _graph.UnitBuses690)
            {
                bus690.CollectFromContributors(context);
                _graph.Bus35.AddPower(bus690.TotalActivePowerKw, bus690.TotalReactivePowerKvar);
            }
        }

        /// <summary>③ 电网读全站 Q，Q-U 定 220kV 电压（电压源唯一职责）。</summary>
        private void Phase3GridVoltageSolve(
            DeviceStepContext context,
            TimeSpan step,
            double? totalReactiveKvarOverride = null)
        {
            double totalQ = totalReactiveKvarOverride ?? _graph.Bus35.TotalReactivePowerKvar;
            _network.Grid.SetAggregatedReactivePowerKvar(totalQ);
            _grid.Activate(context, step);

            _graph.BusGrid.LineVoltageV = _network.Grid.Port.Output.Ac?.Internal.LineVoltageV ?? 0;
            SystemFrequencyResolver.Refresh(_network, context);
            _graph.BusGrid.FrequencyHz = _network.SystemFrequencyHz;
        }

        /// <summary>④ 电压自上而下：经 Coupler 链 Grid → 主断 → 主变 → 35kV → 单元 → 690V。</summary>
        private void Phase4PropagateVoltageTopDown(DeviceStepContext context, TimeSpan step)
        {
            var sweep = BuildSweepContext(context, step);

            if (!context.MainBreakerClosed)
            {
                _network.PccLineVoltageV = 0;
                double islandV = EstimateBus35WhenMainOpen();
                _graph.PropagateVoltageIsland(sweep, islandV);
                _lastBus35LineVoltageV = islandV;
                return;
            }

            double v220 = _graph.BusGrid.LineVoltageV;
            _network.PccLineVoltageV = v220;
            _network.StationBus35LineVoltageV = GridFeedbackConventions.DeriveStationBusVoltage(
                v220, _pccCfg.NominalLineVoltage, _pccCfg.StationBusNominalLineVoltage);

            _graph.PropagateVoltageFromGrid(sweep);
            _lastBus35LineVoltageV = _graph.Bus35.LineVoltageV;
        }

        private PropagationSweepContext BuildSweepContext(DeviceStepContext context, TimeSpan step) =>
            new()
            {
                DeviceContext = context,
                Step = step,
                Bus35 = _graph.Bus35,
                PcsCfg = _pcsCfg,
                SystemFrequencyHz = _network.SystemFrequencyHz,
                LastBus35LineVoltageV = _lastBus35LineVoltageV,
                StationBusNominalLineVoltageV = _pccCfg.StationBusNominalLineVoltage,
                MainBreakerClosed = context.MainBreakerClosed
            };

        /// <summary>⑤ 在已知母线电压下由 P/Q 算电流，驱动各设备 Step。</summary>
        private void Phase5AssignCurrentsAndStepDevices(DeviceStepContext context, TimeSpan step)
        {
            double bus35V = _graph.Bus35.LineVoltageV;

            PropagationPortBinding.SetAcVoltageInput(
                _network.Load.Port, bus35V, ThreePhaseConnection.Star);
            _network.Load.Step(context, step);

            for (int u = 0; u < _graph.UnitBuses690.Count; u++)
            {
                if (u >= _network.UnitBreakers.Count)
                    continue;

                bool unitClosed = _network.UnitBreakers[u].SwitchState.IsClosed
                    && !_network.UnitBreakers[u].SwitchState.IsTripped;
                double bus690V = _graph.UnitBuses690[u].LineVoltageV;
                bool gridAvailable = context.MainBreakerClosed && unitClosed;

                int baseChannel = u * 2;
                for (int ch = 0; ch < 2; ch++)
                {
                    int idx = baseChannel + ch;
                    if (idx >= _network.PcsDevices.Count)
                        continue;

                    SolvePcsBmsPair(context, step, idx, bus690V, gridAvailable);
                }
            }
        }

        /// <summary>设备 Step 后，按实测 Q 多轮 Q-U 定压并完整重跑电压传播直至收敛。</summary>
        private void RunQuvRefinementIterations(DeviceStepContext context, TimeSpan step)
        {
            if (!context.MainBreakerClosed || _quvMaxIterations <= 1)
                return;

            for (int i = 1; i < _quvMaxIterations; i++)
            {
                double totalQ = CollectFeedbackReactivePowerKvar();
                double prevGridV = _graph.BusGrid.LineVoltageV;

                Phase3GridVoltageSolve(context, step, totalQ);
                Phase4PropagateVoltageTopDown(context, step);

                if (QuvConvergence.IsLineVoltageConverged(
                        prevGridV,
                        _graph.BusGrid.LineVoltageV,
                        _pccCfg.NominalLineVoltage,
                        _voltageTolerancePu))
                {
                    // _log.Debug($"[RadialSweep] Q-U/V 反馈迭代 {i} 轮后电压收敛");
                    break;
                }
            }
        }

        /// <summary>叶子设备 Step 后，用最新下游母线电压重算串联设备（主变/单元变）端口。</summary>
        private void RefreshSeriesDevicesAfterLeafStep(DeviceStepContext context, TimeSpan step)
        {
            if (!context.MainBreakerClosed)
                return;

            var sweep = BuildSweepContext(context, step);
            double bus35V = _graph.Bus35.LineVoltageV;
            var stationCurrent = _graph.ResolveStationSecondaryCurrent(sweep);

            PropagationPortBinding.SetAcVoltageInput(
                _network.MainTransformer.Primary,
                _graph.BusAfterMainBreaker.LineVoltageV,
                ThreePhaseConnection.Star);
            PropagationPortBinding.SetAcQuantitiesInput(_network.MainTransformer.Secondary, stationCurrent);
            _network.MainTransformer.Step(context, step);

            for (int u = 0; u < _graph.UnitBuses690.Count; u++)
            {
                if (u >= _network.UnitBreakers.Count || u >= _network.UnitTransformers.Count)
                    continue;

                bool unitClosed = _network.UnitBreakers[u].SwitchState.IsClosed
                    && !_network.UnitBreakers[u].SwitchState.IsTripped;
                var bus690 = _graph.UnitBuses690[u];
                var unitCurrent = bus690.TotalLineCurrentA > 1e-6 || Math.Abs(bus690.TotalPhaseAngleDeg) > 1e-6
                    ? AcQuantityConverter.FromLineVoltageAndPower(
                        Math.Max(bus690.LineVoltageV, _pcsCfg.AcVoltageNominal * 0.01),
                        bus690.TotalActivePowerKw,
                        bus690.TotalReactivePowerKvar,
                        ThreePhaseConnection.Star,
                        _network.SystemFrequencyHz)
                    : new AcInternalQuantities
                    {
                        LineVoltageV = Math.Max(bus690.LineVoltageV, unitClosed ? _pcsCfg.AcVoltageNominal * 0.01 : 0)
                    };

                PropagationPortBinding.SetAcVoltageInput(
                    _network.UnitTransformers[u].Primary,
                    unitClosed ? bus35V : 0,
                    ThreePhaseConnection.Star);
                PropagationPortBinding.SetAcQuantitiesInput(_network.UnitTransformers[u].Secondary, unitCurrent);
                _network.UnitTransformers[u].Step(context, step);
            }
        }

        /// <summary>汇总设备 Step 后的全站无功（并网点 Q-U 反馈，不含 Phase2 意图重复计数）。</summary>
        private double CollectFeedbackReactivePowerKvar()
        {
            double totalQ = _network.Load.Port.Output.Ac?.Internal.ReactivePowerKvar ?? 0;
            foreach (var pcs in _network.PcsDevices)
                totalQ += pcs.GetCurrentState().ReactivePower;
            return totalQ;
        }

        private void SolvePcsBmsPair(
            DeviceStepContext context,
            TimeSpan step,
            int channelIndex,
            double bus690V,
            bool gridAvailable)
        {
            var pcs = _network.PcsDevices[channelIndex];
            var bms = _network.BmsDevices[channelIndex];
            var link = _network.DcLinks[channelIndex];

            bms.IsLinked = link.IsClosed;
            pcs.SetGridAvailable(gridAvailable && bus690V > 1.0);

            PropagationPortBinding.SetAcVoltageInput(pcs.Ac, bus690V, ThreePhaseConnection.Star);
            pcs.Step(context, step);

            var dcCurrentA = pcs.Dc.Output.Dc?.CurrentA ?? 0;
            bms.ApplyDcInputFromPcs(dcCurrentA);
            bms.Step(context, step);

            var dcVoltage = bms.Port.Output.Dc ?? new DcSnapshot();
            pcs.Dc.Input = ElectricalPortSnapshot.FromDc(new DcSnapshot
            {
                VoltageV = link.IsClosed ? dcVoltage.VoltageV : 0,
                CurrentA = dcCurrentA
            });
            pcs.Step(context, step);
        }

        private void SamplePccMeter(DeviceStepContext context, TimeSpan integrationStep, double bus35V)
        {
            double systemF = _network.SystemFrequencyHz;
            AcInternalQuantities primarySample;
            if (context.MainBreakerClosed)
            {
                var raw = _network.MainTransformer.Primary.Output.Ac!.Internal;
                primarySample = new AcInternalQuantities
                {
                    Connection = raw.Connection,
                    LineVoltageV = raw.LineVoltageV,
                    LineCurrentA = raw.LineCurrentA,
                    PhaseAngleDeg = raw.PhaseAngleDeg,
                    FrequencyHz = systemF
                };
            }
            else
            {
                var raw = _network.MainTransformer.Primary.Output.Ac!.Internal;
                primarySample = new AcInternalQuantities
                {
                    Connection = raw.Connection,
                    LineVoltageV = raw.LineVoltageV,
                    LineCurrentA = 0,
                    PhaseAngleDeg = 0,
                    FrequencyHz = systemF
                };
            }

            _network.PccMeter.SampleFrom(primarySample, integrationStep);
        }

        private void PublishBusQuantities()
        {
            SetBusQuantity("BUS_GRID", _network.Grid.Port.Output.Ac!.Internal);
            SetBusQuantity("BUS_35", new AcInternalQuantities
            {
                Connection = ThreePhaseConnection.Star,
                LineVoltageV = _graph.Bus35.LineVoltageV,
                FrequencyHz = _network.SystemFrequencyHz
            });
        }

        private void SetBusQuantity(string busId, AcInternalQuantities qty)
        {
            var bus = _network.GetBus(busId);
            if (bus != null)
                bus.BusQuantity = qty;
        }

        private double EstimateBus35WhenMainOpen()
        {
            if (!NetworkControlBridge.IsBreakerClosed(_network.MainBreaker))
            {
                return EssIslandBusLogic.EstimateIslandedBus35LineVoltageV(
                    _ess._unitTransformers,
                    _ess._unitBreakers,
                    _ess._pcsList);
            }

            double max690 = 0;
            foreach (var xfmr in _network.UnitTransformers)
                max690 = Math.Max(max690, xfmr.Secondary.Output.Ac?.Internal.LineVoltageV ?? 0);

            if (max690 <= 1.0 || _network.UnitTransformers.Count == 0)
                return 0;

            double ratio = _pccCfg.StationBusNominalLineVoltage / Math.Max(_pcsCfg.AcVoltageNominal, 1.0);
            return max690 * ratio;
        }

        private DeviceStepContext BuildContext(DateTime simTime, TimeSpan step) =>
            new()
            {
                SimulationTime = simTime,
                Step = step,
                MainBreakerClosed = _network.MainBreaker.SwitchState.IsClosed
                    && !_network.MainBreaker.SwitchState.IsTripped,
                UtilityGridAvailable = _network.MainBreaker.SwitchState.IsClosed
            };
    }
}

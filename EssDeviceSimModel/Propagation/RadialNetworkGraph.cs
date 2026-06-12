using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Solver;
using log4net;

namespace EssSimulator.EssDeviceSimModel.Propagation
{
    /// <summary>
    /// 从 <see cref="ElectricalNetwork"/> 构建径向母线拓扑、功率贡献者与电压 Coupler 链。
    /// </summary>
    public sealed class RadialNetworkGraph
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(RadialNetworkGraph));
        private readonly PccConfig _pccCfg;
        private readonly List<IBusCoupler> _couplers = new();

        public RadialNetworkGraph(
            ElectricalNetwork network,
            PccConfig pccCfg,
            PcsPhysicalConfig pcsCfg)
        {
            Network = network;
            _pccCfg = pccCfg;
            PcsCfg = pcsCfg;

            BusGrid = new ElectricalBusNode("BUS_GRID", pccCfg.NominalLineVoltage);
            BusAfterMainBreaker = new ElectricalBusNode("BUS_MAIN_SEC", pccCfg.NominalLineVoltage);
            Bus35 = new ElectricalBusNode("BUS_35", pccCfg.StationBusNominalLineVoltage);

            var unitBuses = new List<ElectricalBusNode>();
            int unitCount = network.UnitTransformers.Count;
            for (int u = 0; u < unitCount; u++)
            {
                unitBuses.Add(new ElectricalBusNode(
                    $"BUS_690_U{u}",
                    pcsCfg.AcVoltageNominal));
            }

            UnitBuses690 = unitBuses;

            RegisterContributors(network);
            RegisterVoltageSources(network);
            WireCouplers(network);
            _log.Info(
                $"[RadialGraph] 贡献者 35kV={Bus35.Contributors.Count} / 690V={UnitBuses690.Sum(b => b.Contributors.Count)}，Coupler={_couplers.Count}");
        }

        public ElectricalNetwork Network { get; }
        public PcsPhysicalConfig PcsCfg { get; }
        public ElectricalBusNode BusGrid { get; }
        public ElectricalBusNode BusAfterMainBreaker { get; }
        public ElectricalBusNode Bus35 { get; }
        public IReadOnlyList<ElectricalBusNode> UnitBuses690 { get; }
        public IReadOnlyList<IBusCoupler> Couplers => _couplers;

        /// <summary>从电网母线发起电压传播（并网）。</summary>
        public void PropagateVoltageFromGrid(PropagationSweepContext sweep)
        {
            BusGrid.SetVoltage(
                BusGrid.LineVoltageV,
                BusGrid.FrequencyHz,
                sweep);
            ApplyLocalVoltageSources(sweep);
        }

        /// <summary>离网/主断分：直接设定 35kV 母线并传播至各单元 690V。</summary>
        public void PropagateVoltageIsland(PropagationSweepContext sweep, double bus35LineVoltageV)
        {
            Bus35.SetVoltage(
                bus35LineVoltageV,
                bus35LineVoltageV > 1.0 ? PcsCfg.FrequencyNominal : 0,
                sweep);
            ApplyLocalVoltageSources(sweep);
        }

        /// <summary>在各 690V 母线上合并黑启动 PCS 等本地电压源。</summary>
        public void ApplyLocalVoltageSources(PropagationSweepContext sweep)
        {
            foreach (var bus690 in UnitBuses690)
                bus690.ApplyLocalVoltageSources(sweep);
        }

        internal AcInternalQuantities ResolveStationSecondaryCurrent(PropagationSweepContext sweep)
        {
            double v35Estimate = sweep.LastBus35LineVoltageV > 1.0
                ? sweep.LastBus35LineVoltageV
                : sweep.StationBusNominalLineVoltageV;

            return AcQuantityConverter.FromLineVoltageAndPower(
                v35Estimate,
                sweep.Bus35.TotalActivePowerKw,
                sweep.Bus35.TotalReactivePowerKvar,
                ThreePhaseConnection.Star,
                sweep.PcsCfg.FrequencyNominal);
        }

        private void RegisterContributors(ElectricalNetwork network)
        {
            Bus35.RegisterContributor(new LoadBusContributor(network.Load));

            for (int u = 0; u < UnitBuses690.Count; u++)
            {
                int baseChannel = u * 2;
                for (int ch = 0; ch < 2; ch++)
                {
                    int idx = baseChannel + ch;
                    if (idx >= network.PcsDevices.Count)
                        continue;

                    UnitBuses690[u].RegisterContributor(new PcsBusContributor(network.PcsDevices[idx]));
                }
            }
        }

        private void RegisterVoltageSources(ElectricalNetwork network)
        {
            for (int u = 0; u < UnitBuses690.Count; u++)
            {
                int baseChannel = u * 2;
                for (int ch = 0; ch < 2; ch++)
                {
                    int idx = baseChannel + ch;
                    if (idx >= network.PcsDevices.Count)
                        continue;

                    UnitBuses690[u].RegisterVoltageSource(new PcsBusVoltageSource(network.PcsDevices[idx]));
                }
            }
        }

        private void WireCouplers(ElectricalNetwork network)
        {
            _couplers.Add(new BreakerBusCoupler(
                network.MainBreaker,
                BusGrid,
                BusAfterMainBreaker,
                ResolveStationSecondaryCurrent));

            _couplers.Add(new TransformerBusCoupler(
                network.MainTransformer,
                BusAfterMainBreaker,
                Bus35,
                ResolveStationSecondaryCurrent,
                sweep => sweep.MainBreakerClosed));

            for (int u = 0; u < UnitBuses690.Count; u++)
            {
                if (u >= network.UnitBreakers.Count || u >= network.UnitTransformers.Count)
                    continue;

                _couplers.Add(new UnitBranchCoupler(
                    u,
                    network.UnitBreakers[u],
                    network.UnitTransformers[u],
                    Bus35,
                    UnitBuses690[u]));
            }

            foreach (var coupler in _couplers)
                coupler.Attach();
        }
    }
}

using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Battery;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.EssDeviceSimModel.Solver
{
    public static class NetworkTopologyBuilder
    {
        public static ElectricalNetwork Build(
            SimulatorConfig simCfg,
            PcsPhysicalConfig pcsCfg,
            TransformerConfig mainTransCfg,
            UnitTransformerConfig unitTransCfg,
            LoadConfig loadCfg,
            PccConfig pccCfg,
            BreakerConfig? breakerCfg = null,
            MeterConfig? meterCfg = null,
            IReadOnlyList<BmsRackDevice>? bmsRackDevices = null,
            IReadOnlyList<PcsDevice>? externalPcsDevices = null,
            TransformerDevice? externalMainTransformer = null,
            IReadOnlyList<TransformerDevice>? externalUnitTransformers = null,
            LoadDevice? externalLoadDevice = null,
            MeterSimulator? externalPccMeter = null,
            EnergyStorageSystem? legacyEss = null)
        {
            breakerCfg ??= new BreakerConfig();
            meterCfg ??= new MeterConfig();

            int unitCount = Math.Max(1, simCfg.Devices?.Count ?? 1);
            int channelCount = Math.Max(2, unitCount * 2);

            var gridConfig = new GridConfig
            {
                NominalLineVoltageV = pccCfg.NominalLineVoltage,
                ShortCircuitMva = pccCfg.ShortCircuitMva,
                MaxVoltageShiftPercent = pccCfg.MaxVoltageShiftPercent,
                ReactiveVoltageInfluenceCoefficient = pccCfg.ReactiveVoltageInfluenceCoefficient,
                Connection = ThreePhaseConnection.Star
            };

            var mainTransDeviceCfg = TransformerDeviceFactory.CreateConfig(mainTransCfg);
            var unitTransDeviceCfg = TransformerDeviceFactory.CreateConfig(unitTransCfg);

            var loadDeviceCfg = new LoadDeviceConfig
            {
                ActivePowerKw = loadCfg.ActivePowerPlan,
                ReactivePowerKvar = loadCfg.ReactivePowerPlan,
                Connection = ThreePhaseConnection.Star
            };

            var topology = BuildTopology(unitCount, pccCfg, unitTransCfg);
            var dcLinks = BuildDcLinks(unitCount);

            var grid = new GridSimulator("grid", gridConfig);
            var mainBreaker = new BreakerSimulator("main_breaker", breakerCfg.Main);
            var mainTransformer = externalMainTransformer
                ?? TransformerDeviceFactory.Create("main_transformer", mainTransDeviceCfg);
            var load = externalLoadDevice ?? new LoadDevice("load_35", loadDeviceCfg);
            var meter = externalPccMeter ?? new MeterSimulator("pcc_meter", meterCfg.PccMeter);

            var unitBreakers = new List<BreakerSimulator>();
            var unitTransformers = new List<TransformerDevice>();
            var networkPcsDevices = new List<PcsDevice>();
            var bmsDevices = new List<BmsRackDevice>();

            for (int u = 0; u < unitCount; u++)
            {
                unitBreakers.Add(new BreakerSimulator($"unit_breaker_u{u}", breakerCfg.Unit));
                if (externalUnitTransformers != null && u < externalUnitTransformers.Count)
                    unitTransformers.Add(externalUnitTransformers[u]);
                else
                    unitTransformers.Add(TransformerDeviceFactory.Create($"unit_transformer_u{u}", unitTransDeviceCfg));

                for (int ch = 0; ch < 2; ch++)
                {
                    int channel = u * 2 + ch;
                    if (externalPcsDevices != null && channel < externalPcsDevices.Count)
                        networkPcsDevices.Add(externalPcsDevices[channel]);
                    else
                        networkPcsDevices.Add(PcsDeviceFactory.Create(
                            $"pcs_u{u}_ch{ch}",
                            PcsDeviceFactory.CreateConfig(pcsCfg, simCfg.Runtime.PcsRamp, simCfg.Speedup)));
                    if (bmsRackDevices != null && channel < bmsRackDevices.Count)
                        bmsDevices.Add(bmsRackDevices[channel]);
                    else
                        bmsDevices.Add(new BmsRackDevice($"bms_u{u}_ch{ch}", BmsRackFactory.CreateRack(new Configuration.BmsDeviceConfig())));
                }
            }

            var network = new ElectricalNetwork
            {
                Topology = topology,
                Grid = grid,
                MainBreaker = mainBreaker,
                MainTransformer = mainTransformer,
                Load = load,
                PccMeter = meter,
                UnitBreakers = unitBreakers,
                UnitTransformers = unitTransformers,
                PcsDevices = networkPcsDevices,
                BmsDevices = bmsDevices,
                DcLinks = dcLinks,
                PccLineVoltageV = pccCfg.NominalLineVoltage,
                StationBus35LineVoltageV = pccCfg.StationBusNominalLineVoltage
            };

            network.Solver = new NetworkSolver(network, pccCfg, pcsCfg, legacyEss);
            return network;
        }

        private static NetworkTopology BuildTopology(
            int unitCount,
            PccConfig pccCfg,
            UnitTransformerConfig unitTransCfg)
        {
            var config = new ElectricalTopologyConfig
            {
                Version = 1,
                DefaultAcConnection = ThreePhaseConnection.Star,
                DefaultFrequencyHz = 50,
                Buses =
                {
                    new BusDefinition
                    {
                        Id = "BUS_GRID",
                        NominalLineVoltageV = pccCfg.NominalLineVoltage,
                        Connection = ThreePhaseConnection.Star,
                        Description = "220kV PCC"
                    },
                    new BusDefinition
                    {
                        Id = "BUS_AFTER_MAIN_BRK",
                        NominalLineVoltageV = pccCfg.NominalLineVoltage,
                        Connection = ThreePhaseConnection.Star
                    },
                    new BusDefinition
                    {
                        Id = "BUS_35",
                        NominalLineVoltageV = pccCfg.StationBusNominalLineVoltage,
                        Connection = ThreePhaseConnection.Star,
                        Description = "35kV station bus"
                    }
                },
                SeriesLinks =
                {
                    new SeriesLinkDefinition
                    {
                        LinkId = "L_MAIN_BRK",
                        DeviceId = "main_breaker",
                        DeviceKind = ElectricalDeviceKind.Breaker,
                        UpstreamBusId = "BUS_GRID",
                        DownstreamBusId = "BUS_AFTER_MAIN_BRK"
                    },
                    new SeriesLinkDefinition
                    {
                        LinkId = "L_MAIN_XFMR",
                        DeviceId = "main_transformer",
                        DeviceKind = ElectricalDeviceKind.Transformer,
                        UpstreamBusId = "BUS_AFTER_MAIN_BRK",
                        DownstreamBusId = "BUS_35"
                    }
                },
                MeasurementTaps =
                {
                    new MeasurementTapDefinition
                    {
                        MeterDeviceId = "pcc_meter",
                        SourceDeviceId = "main_transformer",
                        SourcePortId = "primary"
                    }
                }
            };

            for (int u = 0; u < unitCount; u++)
            {
                config.Buses.Add(new BusDefinition
                {
                    Id = $"BUS_35_U{u}",
                    NominalLineVoltageV = pccCfg.StationBusNominalLineVoltage,
                    Connection = ThreePhaseConnection.Star
                });

                config.Buses.Add(new BusDefinition
                {
                    Id = $"BUS_690_U{u}",
                    NominalLineVoltageV = unitTransCfg.SecondaryVoltage,
                    Connection = ThreePhaseConnection.Star,
                    Description = $"Unit-{u + 1} 690V bus"
                });

                config.SeriesLinks.Add(new SeriesLinkDefinition
                {
                    LinkId = $"L_UNIT{u}_BRK",
                    DeviceId = $"unit_breaker_u{u}",
                    DeviceKind = ElectricalDeviceKind.Breaker,
                    UpstreamBusId = "BUS_35",
                    DownstreamBusId = $"BUS_35_U{u}"
                });

                config.SeriesLinks.Add(new SeriesLinkDefinition
                {
                    LinkId = $"L_UNIT{u}_XFMR",
                    DeviceId = $"unit_transformer_u{u}",
                    DeviceKind = ElectricalDeviceKind.Transformer,
                    UpstreamBusId = $"BUS_35_U{u}",
                    DownstreamBusId = $"BUS_690_U{u}"
                });
            }

            return ElectricalTopologyFactory.FromConfig(config);
        }

        private static List<DcLink> BuildDcLinks(int unitCount)
        {
            var links = new List<DcLink>();
            for (int u = 0; u < unitCount; u++)
            {
                for (int ch = 0; ch < 2; ch++)
                {
                    links.Add(new DcLink
                    {
                        LinkId = $"DC_U{u}_CH{ch}",
                        PcsDeviceId = $"pcs_u{u}_ch{ch}",
                        BmsDeviceId = $"bms_u{u}_ch{ch}",
                        DefaultClosed = true,
                        IsClosed = true
                    });
                }
            }

            return links;
        }
    }
}

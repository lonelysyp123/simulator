using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Plant;

public class PlantEngineCharacterizationTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(200);
    private static readonly DateTime SimTime = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static EnergyStorageSystem CreateEss(
        SimulatorConfig? simCfg = null,
        string? meterSourceBusId = null)
    {
        simCfg ??= new SimulatorConfig
        {
            Devices = { new EssUnitConfig() }
        };
        return new EnergyStorageSystem(
            simCfg,
            new PcsPhysicalConfig { AcVoltageNominal = 690 },
            new TransformerConfig(),
            new UnitTransformerConfig(),
            new LoadConfig(),
            new PccConfig(),
            new MeterConfig
            {
                PccMeter = new MeterInstanceConfig
                {
                    SourceBusId = meterSourceBusId ?? RuntimeBusIds.AfterMainBreaker
                }
            });
    }

    [Fact]
    public void MainBreakerClosed_PccAndStationVoltageNearNominal()
    {
        using var ess = CreateEss();
        ess.SetMainBreakerClosed(true);
        ess.PlantEngine.Step(SimTime, Step, Step);

        Assert.InRange(ess.ElectricalNetwork.PccLineVoltageV, 210_000, 230_000);
        Assert.InRange(ess.ElectricalNetwork.StationBus35LineVoltageV, 33_000, 37_000);
    }

    [Fact]
    public void MainBreakerOpen_PccVoltageZero_GridStaysLive()
    {
        using var ess = CreateEss();
        ess.SetMainBreakerClosed(true);
        ess.PlantEngine.Step(SimTime, Step, Step);

        ess.SetMainBreakerClosed(false);
        ess.PlantEngine.Step(SimTime, Step, Step);

        Assert.Equal(0, ess.ElectricalNetwork.PccLineVoltageV);
        Assert.True(ess.ElectricalNetwork.Grid.Port.Output.Ac!.Internal.LineVoltageV > 200_000);
    }

    [Fact]
    public void MeterOnAfterMainBreaker_OpenBreaker_ReportsZero_ClosedHasVoltage()
    {
        using var ess = CreateEss(meterSourceBusId: RuntimeBusIds.AfterMainBreaker);
        ess.SetMainBreakerClosed(true);
        ess.PlantEngine.Step(SimTime, Step, Step);
        Assert.True(ess.ElectricalNetwork.PccMeter.Telemetry.Primary.LineVoltageV > 1000);

        ess.SetMainBreakerClosed(false);
        ess.PlantEngine.Step(SimTime, Step, Step);

        Assert.Equal(0, ess.ElectricalNetwork.PccMeter.Telemetry.Primary.LineVoltageV);
        Assert.Equal(0, ess.ElectricalNetwork.PccMeter.Telemetry.Primary.LineCurrentA);
    }

    [Fact]
    public void UnitWithFourPcs_GridStateReachesAllChannels()
    {
        var simCfg = new SimulatorConfig
        {
            Devices =
            {
                new EssUnitConfig
                {
                    Pcs =
                    {
                        new EssSimulator.Configuration.PcsDeviceConfig(),
                        new EssSimulator.Configuration.PcsDeviceConfig(),
                        new EssSimulator.Configuration.PcsDeviceConfig(),
                        new EssSimulator.Configuration.PcsDeviceConfig()
                    }
                }
            }
        };

        using var ess = CreateEss(simCfg);
        ess.SetMainBreakerClosed(true);
        ess.PlantEngine.Step(SimTime, Step, Step);

        Assert.Equal(4, ess._pcsList.Count);
        for (int i = 0; i < ess._pcsList.Count; i++)
            Assert.True(ess._pcsList[i].IsGridElectricallyAvailable, $"pcs{i + 1} 未收到网侧可用状态");
    }

    [Fact]
    public void RadialGraph_is_always_constructed()
    {
        using var ess = CreateEss();

        Assert.NotNull(ess.RadialGraph);
        Assert.NotNull(ess.PowerSweepEngine);
        ess.SetMainBreakerClosed(true);
        ess.PlantEngine.Step(SimTime, Step, Step);
        Assert.InRange(ess.ElectricalNetwork.PccLineVoltageV, 210_000, 230_000);
    }

    [Fact]
    public void Step_InvokesAfterPlantStep_Once()
    {
        using var ess = CreateEss();
        int calls = 0;
        var hook = new CountingAfterPlantStep(ess, () => calls++);
        AfterPlantStep.Current = hook;
        try
        {
            ess.SetMainBreakerClosed(true);
            ess.PlantEngine.Step(SimTime, Step, Step);
            Assert.Equal(1, calls);
        }
        finally
        {
            hook.Detach();
        }
    }

    private sealed class CountingAfterPlantStep : IAfterPlantStep
    {
        private readonly EnergyStorageSystem _ess;
        private readonly Action _onCall;
        public CountingAfterPlantStep(EnergyStorageSystem ess, Action onCall)
        {
            _ess = ess;
            _onCall = onCall;
        }

        public void AfterPlantStep(EnergyStorageSystem ess, DateTime simTime, TimeSpan elapsed)
        {
            if (ReferenceEquals(ess, _ess))
                _onCall();
        }

        public void Detach()
        {
            if (ReferenceEquals(EssDeviceSimModel.AfterPlantStep.Current, this))
                EssDeviceSimModel.AfterPlantStep.Reset();
        }
    }
}

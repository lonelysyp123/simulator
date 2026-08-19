using EssSimulator.Configuration;

namespace EssSimulator.Tests;

public class SimulatorConfigTests
{
    [Fact]
    public void EmptyDevices_WithoutPv_FallsBackToOneEssUnit()
    {
        var cfg = new SimulatorConfig();
        Assert.Equal(1, cfg.EffectiveEssUnitCount);
        Assert.Equal(2, cfg.UnitCount);
        Assert.Equal(2, cfg.GetBmsDeviceConfigs().Count);
        Assert.Equal(2, cfg.GetPcsDeviceConfigs().Count);
    }

    [Fact]
    public void PvOnly_DoesNotFabricateEssUnits()
    {
        var cfg = new SimulatorConfig
        {
            PvUnits = { new PvUnitRuntimeConfig { Name = "光伏单元-1" } }
        };
        Assert.Equal(0, cfg.EffectiveEssUnitCount);
        Assert.Equal(0, cfg.UnitCount);
        Assert.Equal(1, cfg.PvUnitCount);
        Assert.Empty(cfg.GetBmsDeviceConfigs());
        Assert.Empty(cfg.GetPcsDeviceConfigs());
    }

    [Fact]
    public void MixedPlant_KeepsEssAndPvCounts()
    {
        var cfg = new SimulatorConfig
        {
            Devices = { new EssUnitConfig { Name = "Unit-A" } },
            PvUnits = { new PvUnitRuntimeConfig { Name = "光伏单元-1" } }
        };
        Assert.Equal(1, cfg.EffectiveEssUnitCount);
        Assert.Equal(2, cfg.UnitCount);
        Assert.Equal(1, cfg.PvUnitCount);
        Assert.Equal(2, cfg.GetBmsDeviceConfigs().Count);
    }
}

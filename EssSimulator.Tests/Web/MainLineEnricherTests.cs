using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.Display;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.Web;
using Xunit;

namespace EssSimulator.Tests.Web;

public class MainLineEnricherTests
{
    [Fact]
    public void ResolveEssChannelCount_zero_when_pv_only()
    {
        Assert.Equal(0, MainLineEnricher.ResolveEssChannelCount(pcsCount: 0, pvCount: 2));
        Assert.Equal(0, MainLineEnricher.ResolveEssUnitCount(0));
    }

    [Fact]
    public void ResolveEssChannelCount_keeps_pcs_when_mixed_plant()
    {
        Assert.Equal(4, MainLineEnricher.ResolveEssChannelCount(pcsCount: 4, pvCount: 2));
        Assert.Equal(2, MainLineEnricher.ResolveEssUnitCount(4));
    }

    [Fact]
    public void ResolveEssChannelCount_falls_back_to_one_channel_when_plant_empty()
    {
        Assert.Equal(1, MainLineEnricher.ResolveEssChannelCount(pcsCount: 0, pvCount: 0));
        Assert.Equal(1, MainLineEnricher.ResolveEssUnitCount(1));
    }

    [Fact]
    public void EnrichUnit_builds_one_channel_per_slot_beyond_two()
    {
        // 单机组 4 PCS：每个槽位都应产出运行时通道，不再被 A/B 双通道截断
        var cfg = new SimulatorConfig
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
        using var ess = new EnergyStorageSystem(
            cfg,
            new PcsPhysicalConfig { AcVoltageNominal = 690 },
            new TransformerConfig(),
            new UnitTransformerConfig(),
            new LoadConfig(),
            new PccConfig(),
            new MeterConfig());
        SimulatorHost.Instance.Register("ess", ess);

        var channels = new List<PcsChannelSnapshot?>();
        for (int s = 0; s < 4; s++)
            channels.Add(new PcsChannelSnapshot(s, 0, s, default, 0, 0, 690, 0, 50, "GridConnected", "Inactive", false));
        var snap = new MainLineSnapshot
        {
            Units = new List<UnitBranchSnapshot>
            {
                new UnitBranchSnapshot(0, true, false, default, default, null, channels)
            }
        };

        var vm = MainLineEnricher.Build(snap, channelCountOverride: 4);

        var unit = Assert.Single(vm.Units);
        Assert.Equal(4, unit.Channels.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, unit.Channels.Select(c => c.PcsNumber).ToArray());
        Assert.Equal(new[] { 0, 1, 2, 3 }, unit.Channels.Select(c => c.SlotInUnit).ToArray());
        Assert.Equal(4, unit.PcsChannels.Count);
    }
}

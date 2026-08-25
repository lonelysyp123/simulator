using EssSimulator.Configuration;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class EmuPointCatalogLoaderTests
{
    [Fact]
    public void FromPointMap_simEmu1_HasTelemetryAndControlBindings()
    {
        var pointMap = new ModbusPointMap("emu.csv", "simEmu1");
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simEmu1", new DataExchangeOptions());

        Assert.True(catalog.TelemetryPoints.Count > 40);
        Assert.True(catalog.ControlPoints.Count >= 10);

        var startStop = catalog.ControlPoints.First(p => p.ParamName == "yx3");
        Assert.Equal("emu1", startStop.Target.RootKey);
        Assert.Equal(ControlSemantics.Hold, startStop.Semantics);
        Assert.Equal(ControlEffectId.PcsApplyCommands, startStop.Effect);

        var hvBreaker = catalog.ControlPoints.First(p => p.ParamName == "yx0");
        Assert.Equal(ControlEffectId.UnitHighVoltageBreaker, hvBreaker.Effect);
    }

    [Fact]
    public void FromPointMap_AppliesConfiguredSemanticsOverride()
    {
        var pointMap = new ModbusPointMap("emu.csv", "simEmu1");
        var options = new DataExchangeOptions
        {
            ControlSemantics = { ["yx3"] = "Hold" }
        };

        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simEmu1", options);
        var startStop = catalog.ControlPoints.First(p => p.ParamName == "yx3");
        Assert.Equal(ControlSemantics.Hold, startStop.Semantics);
    }

    [Fact]
    public void FromPointMap_GatesEmuBindingsByUnitComposition()
    {
        var pointMap = new ModbusPointMap("emu.csv", "simEmu1");
        // 机组构成：1 台 PCS、无断路器、无电表
        var units = new List<EssUnitConfig>
        {
            new()
            {
                HasUnitBreaker = false,
                HasUnitMeter = false,
                Pcs = { new PcsDeviceConfig() }
            }
        };

        var gated = PointCatalogLoader.FromPointMap(pointMap, "simEmu1", new DataExchangeOptions(), units);
        var ungated = PointCatalogLoader.FromPointMap(pointMap, "simEmu1", new DataExchangeOptions());

        // 无门控（legacy / 未传机组构成）保持原绑定
        Assert.Contains(ungated.ControlPoints, p => p.ParamName == "yx0");
        Assert.Contains(ungated.ControlPoints, p => p.Target.PropertyPath.Contains("PcsList[1]"));

        // 门控：无断路器剔除 PowerOnOff；越界 PCS 剔除；0 号 PCS 保留
        Assert.DoesNotContain(gated.ControlPoints, p => p.ParamName == "yx0");
        Assert.DoesNotContain(gated.ControlPoints, p => p.Target.PropertyPath.Contains("PcsList[1]"));
        Assert.DoesNotContain(gated.TelemetryPoints, p => p.Target.PropertyPath.Contains("PcsList[1]"));
        Assert.Contains(gated.ControlPoints, p => p.ParamName == "yx3");
        Assert.True(gated.TelemetryPoints.Count < ungated.TelemetryPoints.Count);
        // 被剔除点位保持未绑定语义：默认值表仍完整，寄存器维持默认值
        Assert.Equal(ungated.DefaultValues.Count, gated.DefaultValues.Count);
    }
}

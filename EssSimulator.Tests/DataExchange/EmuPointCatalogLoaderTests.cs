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

        var startStop = catalog.ControlPoints.First(p => p.ParamName == "pcs1_startstop");
        Assert.Equal("emu1", startStop.Target.RootKey);
        Assert.Equal(ControlSemantics.Hold, startStop.Semantics);
        Assert.Equal(ControlEffectId.PcsApplyCommands, startStop.Effect);

        var hvBreaker = catalog.ControlPoints.First(p => p.ParamName == "highvoltagebreakeronoff");
        Assert.Equal(ControlEffectId.UnitHighVoltageBreaker, hvBreaker.Effect);
    }

    [Fact]
    public void FromPointMap_AppliesConfiguredSemanticsOverride()
    {
        var pointMap = new ModbusPointMap("emu.csv", "simEmu1");
        var options = new DataExchangeOptions
        {
            ControlSemantics = { ["pcs1_startstop"] = "Hold" }
        };

        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simEmu1", options);
        var startStop = catalog.ControlPoints.First(p => p.ParamName == "pcs1_startstop");
        Assert.Equal(ControlSemantics.Hold, startStop.Semantics);
    }
}

using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class BmsPointCatalogLoaderTests
{
    [Fact]
    public void FromPointMap_simBms1_HasGridConnectControlForYt0()
    {
        var pointMap = new ModbusPointMap("bms_bank.csv", "simBms1", clusterCount: 12);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simBms1", new DataExchangeOptions());

        var yt0 = catalog.ControlPoints.First(p => p.ParamName == "yt0");
        Assert.Equal(1000, yt0.Entry.Address);
        Assert.Equal("u16", yt0.Entry.Type);
        Assert.Equal("bms1", yt0.Target.RootKey);
        Assert.Equal("BatteryStacks[0].GridConnectCommand", yt0.Target.PropertyPath);
        Assert.Equal(ControlSemantics.Pulse, yt0.Semantics);
        Assert.Equal(ControlEffectId.BmsApplyLinkCommands, yt0.Effect);

        Assert.True(catalog.RackTelemetryPoints.Count > 10);
        Assert.Contains("rackId", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
        Assert.StartsWith("bms1.", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
    }

    [Fact]
    public void FromPointMap_IncludesRackControlThresholds()
    {
        var pointMap = new ModbusPointMap("bms_bank.csv", "simBms1", clusterCount: 12);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simBms1");

        Assert.NotEmpty(catalog.RackControlPoints);
        var yc1322 = catalog.FindRackControl("yc1322");
        Assert.NotNull(yc1322);
        Assert.Contains("Thresholds.CellOvervoltageThreshold1", yc1322!.BindingPathTemplate);
        Assert.Equal(
            "bms1.BatteryStacks[0].Cluseter[0].Thresholds.CellOvervoltageThreshold1",
            yc1322.ResolvePath(0));
    }
}

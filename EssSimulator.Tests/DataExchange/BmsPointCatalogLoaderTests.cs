using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class BmsPointCatalogLoaderTests
{
    [Fact]
    public void FromPointMap_simBms1_HasPulseSemanticsForParam11()
    {
        var pointMap = new ModbusPointMap("bms_bank.csv", "simBms1", clusterCount: 12);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simBms1", new DataExchangeOptions());

        var param11 = catalog.ControlPoints.First(p => p.ParamName == "param11");
        Assert.Equal("bms1", param11.Target.RootKey);
        Assert.Equal(ControlSemantics.Pulse, param11.Semantics);
        Assert.Equal(ControlEffectId.BmsApplyLinkCommands, param11.Effect);

        var param12 = catalog.ControlPoints.First(p => p.ParamName == "param12");
        Assert.Equal(ControlSemantics.Hold, param12.Semantics);
        Assert.Equal(ControlEffectId.BmsApplyLinkCommands, param12.Effect);

        Assert.True(catalog.RackTelemetryPoints.Count > 10);
        Assert.Contains("rackId", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
        Assert.StartsWith("bms1.", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
    }
}

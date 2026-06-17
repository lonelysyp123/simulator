using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class BmsPointCatalogLoaderTests
{
    [Fact]
    public void FromPointMap_simBms1_HasGridConnectControlForYc133()
    {
        var pointMap = new ModbusPointMap("bms_bank.csv", "simBms1", clusterCount: 12);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simBms1", new DataExchangeOptions());

        var yc133 = catalog.ControlPoints.First(p => p.ParamName == "yc133");
        Assert.Equal(1001, yc133.Entry.Address);
        Assert.Equal("bms1", yc133.Target.RootKey);
        Assert.Equal(ControlSemantics.Pulse, yc133.Semantics);
        Assert.Equal(ControlEffectId.BmsApplyLinkCommands, yc133.Effect);

        Assert.True(catalog.RackTelemetryPoints.Count > 10);
        Assert.Contains("rackId", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
        Assert.StartsWith("bms1.", catalog.RackTelemetryPoints[0].BindingPathTemplate, StringComparison.Ordinal);
    }
}

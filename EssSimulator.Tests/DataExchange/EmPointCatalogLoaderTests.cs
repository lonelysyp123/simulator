using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class EmPointCatalogLoaderTests
{
    [Fact]
    public void FromPointMap_simEm_HasTelemetryOnly()
    {
        var pointMap = new ModbusPointMap("em.csv", "simEm");
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simEm", new DataExchangeOptions());

        Assert.NotEmpty(catalog.TelemetryPoints);
        Assert.Empty(catalog.ControlPoints);
        Assert.Contains(catalog.TelemetryPoints, p => p.ParamName == "yc12");
    }
}

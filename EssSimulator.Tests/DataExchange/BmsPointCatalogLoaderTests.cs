using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class BmsPointCatalogLoaderTests
{
    /// <summary>
    /// common 版点表绝对路径。根目录 bms_bank.csv 可能随交付版本切换（如 LC 版），
    /// 本测试固定验证 pointmaps/common 版本，不能依赖复制到 bin 的运行时点表。
    /// </summary>
    private static string CommonBankCsvPath
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "EssSimulator.sln")))
                    return Path.Combine(dir.FullName, "pointmaps", "common", "bms_bank.csv");
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("找不到仓库根目录");
        }
    }

    [Fact]
    public void FromPointMap_simBms1_HasGridConnectControlForYt0()
    {
        var pointMap = new ModbusPointMap(CommonBankCsvPath, "simBms1", clusterCount: 12);
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
        var pointMap = new ModbusPointMap(CommonBankCsvPath, "simBms1", clusterCount: 12);
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

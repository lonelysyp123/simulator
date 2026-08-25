using EssSimulator.DataExchange.Catalog;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

/// <summary>
/// LC 系统级点表（trina）升级为 DataExchange 设备后的目录解析验证：
/// 插件点覆盖、emuDeviceId 占位符按聚合组首机组替换、控制点绑定首机组 EMU 虚拟模型。
/// </summary>
public class LcDataExchangeCatalogTests
{
    private static string LcCsvPath =>
        Path.Combine(FindRepoRoot(), "pointmaps", "models", "lc", "trina", "lc.csv");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EssSimulator.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("找不到仓库根目录");
    }

    private static readonly string[] ExpectedPluginWordKeys =
    {
        "SystemFaultSummary", "SystemRunStateSummary", "UnitBlackStartStatus",
        "UnitPcsTotalCount", "UnitPcsRunningCount", "UnitPcsAlarmCount", "UnitPcsFaultCount"
    };

    [Fact]
    public void SimLc1_FirstEmuId1_PluginPointsCoverAllWordKeys()
    {
        var pointMap = new ModbusPointMap(LcCsvPath, "simLc1", clusterCount: 0, emuDeviceIdOverride: 1);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simLc1");

        var wordKeys = catalog.PluginPoints.Select(p => p.WordKey).ToArray();
        Assert.Equal(ExpectedPluginWordKeys.Length, wordKeys.Length);
        foreach (var key in ExpectedPluginWordKeys)
            Assert.Contains(key, wordKeys);

        // emuDeviceId 占位符点（sysyc170/200~203）已替换为首机组根路径 emu1
        Assert.All(catalog.PluginPoints, p => Assert.Equal("emu1", p.DeviceRoot));
    }

    [Fact]
    public void SimLc1_ControlPoints_BindFirstEmuUnitVirtualModel()
    {
        var pointMap = new ModbusPointMap(LcCsvPath, "simLc1", clusterCount: 0, emuDeviceIdOverride: 1);
        var catalog = PointCatalogLoader.FromPointMap(pointMap, "simLc1");

        // 6 个 SYSTEM 控制点（syst4~7、syst1010/1011）全部绑定 emu1 虚拟模型
        Assert.Equal(6, catalog.ControlPoints.Count);
        Assert.All(catalog.ControlPoints, b => Assert.Equal("emu1", b.Target.RootKey));

        var syst6 = Assert.Single(catalog.ControlPoints, b => b.ParamName == "syst6");
        Assert.Equal("Emu.SystemOperation", syst6.Target.PropertyPath);
        // EMU 级系统控制走 PcsApplyCommands 效果链路（与 simEmu 一致）
        Assert.Equal(ControlEffectId.PcsApplyCommands, syst6.Effect);
    }

    [Fact]
    public void EmuDeviceIdOverride_SubstitutesPlaceholder_ForNonEmuDevice()
    {
        // simLc2 聚合组首机组为 emu3（emuPerGroup=2 场景）
        var pointMap = new ModbusPointMap(LcCsvPath, "simLc2", clusterCount: 0, emuDeviceIdOverride: 3);

        var syst6Model = pointMap.ParamModelLookup["syst6"];
        Assert.Equal("emu3.Emu.SystemOperation", syst6Model.Arg1);
        Assert.DoesNotContain("emuDeviceId", syst6Model.Arg1);
    }

    [Fact]
    public void EmuDeviceIdOverride_Null_SimLcPlaceholderUnchanged()
    {
        // 回归：override 未提供且设备名非 Emu 时不做 emuDeviceId 替换
        var pointMap = new ModbusPointMap(LcCsvPath, "simLc1", clusterCount: 0);

        var syst6Model = pointMap.ParamModelLookup["syst6"];
        Assert.Contains("emuDeviceId", syst6Model.Arg1);
    }
}

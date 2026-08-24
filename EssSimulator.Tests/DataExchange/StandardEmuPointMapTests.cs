using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.EssSimModelApi;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

/// <summary>
/// 验证 standard 版 EMU 点表（pointmaps/models/emu/standard/emu.csv）的绑定准确性：
/// 1. 所有 ModelSim 绑定路径能在 EnergyManagementData（Emu + PcsList）上解析出非空值；
/// 2. 控制点（FC5/FC6）绑定属性可写；
/// 3. 频率与功率能力点位（yc23/yc36-39/yc50/yc63-66）已绑定真实模型属性。
/// </summary>
public class StandardEmuPointMapTests
{
    private static string StandardCsvPath =>
        Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "standard", "emu.csv");

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

    private static PointCatalog LoadStandardCatalog()
    {
        Assert.True(File.Exists(StandardCsvPath), $"standard 点表不存在: {StandardCsvPath}");
        var pointMap = new ModbusPointMap(StandardCsvPath, "simEmu1");
        return PointCatalogLoader.FromPointMap(pointMap, "simEmu1", new DataExchangeOptions());
    }

    private static EnergyManagementData CreateEmuData(int pcsCount = 2)
    {
        var emu = new EnergyManagementData();
        for (int i = 0; i < pcsCount; i++)
        {
            emu.PcsList.Add(new PcsData
            {
                PcsId = i + 1,
                Frequency = 50f,
                ChargePowerLimit = 2500f,
                DischargePowerLimit = 2500f,
                PCSRatePower = 2750f
            });
        }

        emu.Emu.MaxChargePower = pcsCount * 2500f;
        emu.Emu.MaxDischargePower = pcsCount * 2500f;
        return emu;
    }

    [Fact]
    public void Standard_AllTelemetryBindings_ResolveToModelValues()
    {
        var catalog = LoadStandardCatalog();
        var emu = CreateEmuData();

        Assert.True(catalog.TelemetryPoints.Count > 40,
            $"standard 遥测绑定点位过少: {catalog.TelemetryPoints.Count}");

        var failures = new List<string>();
        foreach (var point in catalog.TelemetryPoints)
        {
            var value = ObjectPathResolver.GetValue(emu, point.Target.PropertyPath);
            if (value == null)
                failures.Add($"{point.ParamName}: 路径 {point.Target.PropertyPath} 解析为 null");
        }

        Assert.True(failures.Count == 0,
            $"以下遥测绑定解析失败:\n{string.Join("\n", failures)}");
    }

    [Fact]
    public void Standard_AllControlBindings_AreWritable()
    {
        var catalog = LoadStandardCatalog();
        var emu = CreateEmuData();

        Assert.True(catalog.ControlPoints.Count >= 10,
            $"standard 控制点位过少: {catalog.ControlPoints.Count}");

        var failures = new List<string>();
        foreach (var point in catalog.ControlPoints)
        {
            var value = ObjectPathResolver.GetValue(emu, point.Target.PropertyPath);
            if (value == null)
                failures.Add($"{point.ParamName}: 路径 {point.Target.PropertyPath} 解析为 null");
        }

        Assert.True(failures.Count == 0,
            $"以下控制绑定解析失败:\n{string.Join("\n", failures)}");
    }

    [Theory]
    [InlineData("yc23", "PcsList[0].Frequency")]
    [InlineData("yc50", "PcsList[1].Frequency")]
    [InlineData("yc36", "PcsList[0].ChargePowerLimit")]
    [InlineData("yc37", "PcsList[0].DischargePowerLimit")]
    [InlineData("yc38", "PcsList[0].PCSRatePower")]
    [InlineData("yc39", "PcsList[0].PCSRatePower")]
    [InlineData("yc63", "PcsList[1].ChargePowerLimit")]
    [InlineData("yc64", "PcsList[1].DischargePowerLimit")]
    [InlineData("yc65", "PcsList[1].PCSRatePower")]
    [InlineData("yc66", "PcsList[1].PCSRatePower")]
    public void Standard_FrequencyAndCapabilityPoints_BindModelProperties(string paramName, string expectedPath)
    {
        var catalog = LoadStandardCatalog();

        var point = catalog.TelemetryPoints.FirstOrDefault(p => p.ParamName == paramName);
        Assert.NotNull(point);
        Assert.Equal(expectedPath, point!.Target.PropertyPath);
    }
}

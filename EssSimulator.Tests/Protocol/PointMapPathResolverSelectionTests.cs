using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.Protocol;

/// <summary>
/// 设备型号选型生效时的点表解析优先级测试。
/// 选型文件写入测试 bin 的 configs/topology/device-models.json，测试结束清理，
/// 避免污染其他依赖默认兜底解析的用例。
/// </summary>
public class PointMapPathResolverSelectionTests : IDisposable
{
    public PointMapPathResolverSelectionTests()
    {
        CleanupSelection();
    }

    public void Dispose()
    {
        CleanupSelection();
    }

    private static void CleanupSelection()
    {
        try
        {
            var path = DeviceModelRegistry.SelectionFilePath();
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* ignore */ }
        DeviceModelRegistry.InvalidateCache();
    }

    [Fact]
    public void Resolve_WithSelection_PrefersModelDirectory()
    {
        var modelsRoot = DeviceModelRegistry.FindModelsRoot();
        Assert.True(modelsRoot != null && Directory.Exists(Path.Combine(modelsRoot, "pointmaps", "models", "bms", "standard")),
            "测试输出目录应包含 pointmaps/models/bms/standard（csproj 复制）");

        try
        {
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["bms"] = "standard"
                }
            });

            var resolved = PointMapPathResolver.Resolve("bms_bank.csv");
            Assert.Contains(Path.Combine("pointmaps", "models", "bms", "standard"), resolved);
            Assert.EndsWith("bms_bank.csv", resolved);

            // rack 伴随文件与 bank 同目录（型号内配对）
            var rack = PointMapPathResolver.ResolveSibling(resolved, "bms_rack.csv");
            Assert.Equal(Path.GetDirectoryName(resolved), Path.GetDirectoryName(rack));
        }
        finally
        {
            CleanupSelection();
        }
    }

    [Fact]
    public void Resolve_WithoutSelection_FallsBackToStandardModel()
    {
        // 构造器已清理选型文件；未选型时兜底到 standard 型号点表（或根目录运行时副本）
        var resolved = PointMapPathResolver.Resolve("emu.csv");
        Assert.EndsWith("emu.csv", resolved);
        Assert.True(File.Exists(resolved));
    }

    [Fact]
    public void Resolve_UnselectedType_FallsBackToStandardModel()
    {
        try
        {
            // 仅选型 BMS；未选型的设备类型（EMU）仍走兜底解析（standard 型号或根目录副本）
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["bms"] = "standard"
                }
            });

            var resolved = PointMapPathResolver.Resolve("emu.csv");
            Assert.EndsWith("emu.csv", resolved);
            Assert.True(File.Exists(resolved));
        }
        finally
        {
            CleanupSelection();
        }
    }
}

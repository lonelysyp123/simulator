using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.Protocol;

public class DeviceModelRegistryTests
{
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

    [Fact]
    public void ListTypes_ScansKnownTypesAndModels()
    {
        var types = DeviceModelRegistry.ListTypes(FindRepoRoot());
        var byId = types.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        Assert.True(byId.ContainsKey("bms"));
        Assert.True(byId.ContainsKey("emu"));
        Assert.True(byId.ContainsKey("em"));
        Assert.True(byId.ContainsKey("lc"));
        Assert.True(byId.ContainsKey("pv"));

        var bms = byId["bms"];
        Assert.Contains("bms_bank.csv", bms.Files);
        Assert.Contains("bms_rack.csv", bms.Files);

        var pv = byId["pv"];
        Assert.Contains("pv_logger.csv", pv.Files);
        Assert.Contains("pv_apm810.csv", pv.Files);

        var modelIds = bms.Models.Select(m => m.Id).ToList();
        Assert.Contains("standard", modelIds);
        Assert.Contains("g2_pro", modelIds);
        Assert.All(bms.Models, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Name));
            Assert.True(Directory.Exists(m.Directory));
        });
    }

    [Fact]
    public void ListTypes_LcIncludesTrinaMvModels_EmuOnlyStandard()
    {
        var types = DeviceModelRegistry.ListTypes(FindRepoRoot());
        var byId = types.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        var emuIds = byId["emu"].Models.Select(m => m.Id).ToList();
        Assert.Equal(new[] { "standard" }, emuIds);

        var lcIds = byId["lc"].Models.Select(m => m.Id).ToList();
        Assert.Contains("standard", lcIds);
        Assert.Contains("trina_5.5MW", lcIds);
        Assert.Contains("trina_10MW", lcIds);
        Assert.All(byId["lc"].Models, m =>
        {
            Assert.True(File.Exists(Path.Combine(m.Directory, "lc.csv")));
        });
    }

    [Fact]
    public void FindTypeForFile_MapsRuntimeFileNamesToTypes()
    {
        var root = FindRepoRoot();
        Assert.Equal("bms", DeviceModelRegistry.FindTypeForFile("bms_bank.csv", root));
        Assert.Equal("bms", DeviceModelRegistry.FindTypeForFile("bms_rack.csv", root));
        Assert.Equal("emu", DeviceModelRegistry.FindTypeForFile("emu.csv", root));
        Assert.Equal("em", DeviceModelRegistry.FindTypeForFile("em.csv", root));
        Assert.Equal("lc", DeviceModelRegistry.FindTypeForFile("lc.csv", root));
        Assert.Equal("pv", DeviceModelRegistry.FindTypeForFile("pv_logger.csv", root));
        Assert.Equal("pv", DeviceModelRegistry.FindTypeForFile("pv_apm810.csv", root));
    }

    [Fact]
    public void ValidateSelection_ReportsUnknownTypeAndModel()
    {
        var root = FindRepoRoot();

        var ok = DeviceModelRegistry.ValidateSelection(
            new Dictionary<string, string> { ["bms"] = "g2_pro", ["emu"] = "standard" }, root);
        Assert.Empty(ok);

        var bad = DeviceModelRegistry.ValidateSelection(
            new Dictionary<string, string> { ["bms"] = "no-such-model", ["xxx"] = "standard" }, root);
        Assert.Equal(2, bad.Count);
    }

    [Fact]
    public void Selection_SaveLoadRoundtrip_WithExplicitRoot()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ess-devmodel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(tmp, "configs", "topology"));
        try
        {
            var saved = DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["bms"] = "g2_pro"
                }
            }, tmp);

            Assert.True(File.Exists(Path.Combine(tmp, DeviceModelRegistry.SelectionRelativePath)));

            var loaded = DeviceModelRegistry.LoadSelection(tmp);
            Assert.Equal("g2_pro", loaded.Selections["bms"]);
            Assert.Equal(saved.UpdatedAtUtc, loaded.UpdatedAtUtc);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void LoadSelection_ReturnsEmpty_WhenFileMissing()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ess-devmodel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var loaded = DeviceModelRegistry.LoadSelection(tmp);
            Assert.Empty(loaded.Selections);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }
}

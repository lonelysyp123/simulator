using EssSimulator.Protocol.Modbus;
using EssSimulator.Web.Topology;

namespace EssSimulator.Tests.Topology;

/// <summary>工程保存时按 PCS 总数自动选型 LC 点表（2→standard、4→trina_5.5MW、8→trina_10MW）。</summary>
public class LcPointMapAutoSelectTests
{
    [Theory]
    [InlineData(2, "standard")]
    [InlineData(4, "trina_5.5MW")]
    [InlineData(8, "trina_10MW")]
    public void ResolveModelId_KnownPcsCounts_MapsToLcModel(int pcsCount, string expected)
    {
        Assert.Equal(expected, LcPointMapAutoSelect.ResolveModelId(pcsCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(16)]
    public void ResolveModelId_OtherCounts_ReturnsNull(int pcsCount)
    {
        Assert.Null(LcPointMapAutoSelect.ResolveModelId(pcsCount));
    }

    [Fact]
    public void CountPcs_OnlyCountsPcsTemplateNodes()
    {
        var project = BuildProject("pcs", "pcs", "emu_unit", "ac_bus", "pcs");
        Assert.Equal(3, LcPointMapAutoSelect.CountPcs(project));
    }

    [Fact]
    public void ApplyForProject_FourPcs_SelectsTrina55MW_AndPreservesEmuAndBms()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["bms"] = "g2_pro",
                    ["emu"] = "standard"
                }
            }, tmp);

            var applied = LcPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs", "pcs"), tmp);

            Assert.Equal("trina_5.5MW", applied);
            var selection = DeviceModelRegistry.LoadSelection(tmp);
            Assert.Equal("trina_5.5MW", selection.Selections["lc"]);
            Assert.Equal("standard", selection.Selections["emu"]);
            Assert.Equal("g2_pro", selection.Selections["bms"]);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_TwoAndEightPcs_SelectStandardAnd10MW()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            Assert.Equal("standard", LcPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs"), tmp));
            Assert.Equal("standard", DeviceModelRegistry.LoadSelection(tmp).Selections["lc"]);

            var eight = BuildProject(Enumerable.Repeat("pcs", 8).ToArray());
            Assert.Equal("trina_10MW", LcPointMapAutoSelect.ApplyForProject(eight, tmp));
            Assert.Equal("trina_10MW", DeviceModelRegistry.LoadSelection(tmp).Selections["lc"]);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_SameSelectionAgain_ReturnsNullWithoutChange()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            var project = BuildProject("pcs", "pcs");
            Assert.Equal("standard", LcPointMapAutoSelect.ApplyForProject(project, tmp));
            var stamp = DeviceModelRegistry.LoadSelection(tmp).UpdatedAtUtc;

            Assert.Null(LcPointMapAutoSelect.ApplyForProject(project, tmp));
            Assert.Equal(stamp, DeviceModelRegistry.LoadSelection(tmp).UpdatedAtUtc);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_UnmatchedCount_KeepsExistingLcSelection()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["lc"] = "trina_10MW" }
            }, tmp);

            Assert.Null(LcPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs"), tmp));
            Assert.Equal("trina_10MW", DeviceModelRegistry.LoadSelection(tmp).Selections["lc"]);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_StaleEmuTrinaSelection_MovesToLcAndResetsEmu()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["emu"] = "trina_10MW"
                }
            }, tmp);

            var applied = LcPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs"), tmp);

            Assert.Equal("trina_10MW", applied);
            var selection = DeviceModelRegistry.LoadSelection(tmp);
            Assert.Equal("trina_10MW", selection.Selections["lc"]);
            Assert.Equal("standard", selection.Selections["emu"]);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_ModelDirMissing_SkipsSelection()
    {
        var tmp = CreateTempRoot("standard");
        try
        {
            Assert.Null(LcPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs", "pcs"), tmp));
            Assert.False(DeviceModelRegistry.LoadSelection(tmp).Selections.ContainsKey("lc"));
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    private static TopologyProject BuildProject(params string[] templateIds)
    {
        var project = new TopologyProject();
        for (int i = 0; i < templateIds.Length; i++)
            project.Nodes.Add(new TopologyNode { Id = $"n{i}", TemplateId = templateIds[i] });
        return project;
    }

    /// <summary>构造临时根目录：pointmaps/models/lc/{models} + configs/topology。</summary>
    private static string CreateTempRoot(params string[] lcModelIds)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ess-lc-autosel-" + Guid.NewGuid().ToString("N"));
        foreach (var modelId in lcModelIds)
            Directory.CreateDirectory(Path.Combine(tmp, DeviceModelRegistry.ModelsRelativeDir, "lc", modelId));
        Directory.CreateDirectory(Path.Combine(tmp, "configs", "topology"));
        return tmp;
    }
}

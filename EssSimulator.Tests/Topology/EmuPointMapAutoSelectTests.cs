using EssSimulator.Protocol.Modbus;
using EssSimulator.Web.Topology;

namespace EssSimulator.Tests.Topology;

/// <summary>工程保存时按 PCS 总数自动选型 EMU 点表（2→standard、4→trina_5.5MW、8→trina_10MW）。</summary>
public class EmuPointMapAutoSelectTests
{
    [Theory]
    [InlineData(2, "standard")]
    [InlineData(4, "trina_5.5MW")]
    [InlineData(8, "trina_10MW")]
    public void ResolveModelId_KnownPcsCounts_MapsToModel(int pcsCount, string expected)
    {
        Assert.Equal(expected, EmuPointMapAutoSelect.ResolveModelId(pcsCount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    [InlineData(16)]
    public void ResolveModelId_OtherCounts_ReturnsNull(int pcsCount)
    {
        Assert.Null(EmuPointMapAutoSelect.ResolveModelId(pcsCount));
    }

    [Fact]
    public void CountPcs_OnlyCountsPcsTemplateNodes()
    {
        var project = BuildProject("pcs", "pcs", "emu_unit", "ac_bus", "pcs");
        Assert.Equal(3, EmuPointMapAutoSelect.CountPcs(project));
    }

    [Fact]
    public void ApplyForProject_FourPcs_SelectsTrina55MW_AndPreservesOtherSelections()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            // 预置既有选型：BMS 型号不应被覆盖
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["bms"] = "g2_pro" }
            }, tmp);

            var applied = EmuPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs", "pcs"), tmp);

            Assert.Equal("trina_5.5MW", applied);
            var selection = DeviceModelRegistry.LoadSelection(tmp);
            Assert.Equal("trina_5.5MW", selection.Selections["emu"]);
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
            Assert.Equal("standard", EmuPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs"), tmp));
            Assert.Equal("standard", DeviceModelRegistry.LoadSelection(tmp).Selections["emu"]);

            var eight = BuildProject(Enumerable.Repeat("pcs", 8).ToArray());
            Assert.Equal("trina_10MW", EmuPointMapAutoSelect.ApplyForProject(eight, tmp));
            Assert.Equal("trina_10MW", DeviceModelRegistry.LoadSelection(tmp).Selections["emu"]);
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
            Assert.Equal("standard", EmuPointMapAutoSelect.ApplyForProject(project, tmp));
            var stamp = DeviceModelRegistry.LoadSelection(tmp).UpdatedAtUtc;

            Assert.Null(EmuPointMapAutoSelect.ApplyForProject(project, tmp));
            Assert.Equal(stamp, DeviceModelRegistry.LoadSelection(tmp).UpdatedAtUtc);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_UnmatchedCount_KeepsExistingSelection()
    {
        var tmp = CreateTempRoot("standard", "trina_5.5MW", "trina_10MW");
        try
        {
            DeviceModelRegistry.SaveSelection(new DeviceModelSelection
            {
                Selections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["emu"] = "trina_10MW" }
            }, tmp);

            Assert.Null(EmuPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs"), tmp));
            Assert.Equal("trina_10MW", DeviceModelRegistry.LoadSelection(tmp).Selections["emu"]);
        }
        finally
        {
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public void ApplyForProject_ModelDirMissing_SkipsSelection()
    {
        // 部署环境缺少 trina 型号目录时不写入无效选型
        var tmp = CreateTempRoot("standard");
        try
        {
            Assert.Null(EmuPointMapAutoSelect.ApplyForProject(BuildProject("pcs", "pcs", "pcs", "pcs"), tmp));
            Assert.False(DeviceModelRegistry.LoadSelection(tmp).Selections.ContainsKey("emu"));
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

    /// <summary>构造临时根目录：pointmaps/models/emu/{models} + configs/topology。</summary>
    private static string CreateTempRoot(params string[] emuModelIds)
    {
        var tmp = Path.Combine(Path.GetTempPath(), "ess-emu-autosel-" + Guid.NewGuid().ToString("N"));
        foreach (var modelId in emuModelIds)
            Directory.CreateDirectory(Path.Combine(tmp, DeviceModelRegistry.ModelsRelativeDir, "emu", modelId));
        Directory.CreateDirectory(Path.Combine(tmp, "configs", "topology"));
        return tmp;
    }
}

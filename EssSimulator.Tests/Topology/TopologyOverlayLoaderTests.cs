using System.Text.Json;
using EssSimulator.Configuration;
using EssSimulator.Web.Topology;
using Xunit;

namespace EssSimulator.Tests.Topology;

public class TopologyOverlayLoaderTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void IsUsable_requires_units_with_pcs_and_bms()
    {
        Assert.False(TopologyOverlayLoader.IsUsable(null));
        Assert.False(TopologyOverlayLoader.IsUsable(new TopologyRuntimeOverlay()));
        Assert.False(TopologyOverlayLoader.IsUsable(new TopologyRuntimeOverlay
        {
            EssUnits = { new EssUnitConfig { Name = "U1" } }
        }));
        Assert.True(TopologyOverlayLoader.IsUsable(new TopologyRuntimeOverlay
        {
            EssUnits =
            {
                new EssUnitConfig
                {
                    Name = "U1",
                    Pcs = { new PcsDeviceConfig { Name = "PCS-A" } },
                    Bms = { new BmsDeviceConfig { Name = "BMS-A" } }
                }
            }
        }));
    }

    [Fact]
    public void TryLoad_returns_null_when_engineering_mode_off()
    {
        using var tmp = new TempTopologyRoot(engineeringMode: false, overlay: UsableOverlay());
        Assert.Null(TopologyOverlayLoader.TryLoad(tmp.Root));
    }

    [Fact]
    public void TryLoad_returns_null_when_overlay_has_no_units()
    {
        using var tmp = new TempTopologyRoot(engineeringMode: true, overlay: new TopologyRuntimeOverlay());
        Assert.Null(TopologyOverlayLoader.TryLoad(tmp.Root));
    }

    [Fact]
    public void TryLoad_returns_overlay_when_engineering_mode_on_and_usable()
    {
        var expected = UsableOverlay();
        using var tmp = new TempTopologyRoot(engineeringMode: true, overlay: expected);
        var loaded = TopologyOverlayLoader.TryLoad(tmp.Root);
        Assert.NotNull(loaded);
        Assert.Equal(expected.SourceProjectName, loaded!.SourceProjectName);
        Assert.Single(loaded.EssUnits);
    }

    [Fact]
    public void TryLoad_returns_null_when_json_corrupt()
    {
        using var tmp = new TempTopologyRoot(engineeringMode: true, overlay: UsableOverlay());
        File.WriteAllText(tmp.OverlayPath, "{ not-json");
        Assert.Null(TopologyOverlayLoader.TryLoad(tmp.Root));
    }

    [Fact]
    public void Repo_default_engineering_overlay_is_usable()
    {
        var root = FindRepoRoot();
        var modePath = Path.Combine(root, "configs", "topology", "runtime-mode.json");
        if (!File.Exists(modePath))
            return;

        var mode = JsonSerializer.Deserialize<TopologyRuntimeMode>(File.ReadAllText(modePath), JsonOpts);
        if (mode?.EngineeringMode != true)
            return;

        var overlay = TopologyOverlayLoader.TryLoad(root);
        Assert.NotNull(overlay);
        Assert.True(TopologyOverlayLoader.IsUsable(overlay));
        Assert.True(overlay!.EssUnits.Count >= 1);
    }

    private static TopologyRuntimeOverlay UsableOverlay() => new()
    {
        SourceProjectName = "fixture",
        EssUnits =
        {
            new EssUnitConfig
            {
                Name = "Unit-1",
                Pcs = { new PcsDeviceConfig { Name = "PCS-1A" } },
                Bms = { new BmsDeviceConfig { Name = "BMS-1A" } }
            }
        }
    };

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EssSimulator.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("未找到仓库根目录");
    }

    private sealed class TempTopologyRoot : IDisposable
    {
        public string Root { get; }
        public string OverlayPath { get; }

        public TempTopologyRoot(bool engineeringMode, TopologyRuntimeOverlay overlay)
        {
            Root = Path.Combine(Path.GetTempPath(), "ess-overlay-" + Guid.NewGuid().ToString("N"));
            var generated = Path.Combine(Root, "configs", "topology", "generated");
            Directory.CreateDirectory(generated);
            OverlayPath = Path.Combine(generated, "runtime-overlay.json");
            File.WriteAllText(
                Path.Combine(Root, "configs", "topology", "runtime-mode.json"),
                JsonSerializer.Serialize(new TopologyRuntimeMode { EngineeringMode = engineeringMode }, JsonOpts));
            File.WriteAllText(OverlayPath, JsonSerializer.Serialize(overlay, JsonOpts));
        }

        public void Dispose()
        {
            try { Directory.Delete(Root, recursive: true); }
            catch { /* temp */ }
        }
    }
}

using EssSimulator.Protocol.Iec61850;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.Protocol;

public class Iec61850MappingTests
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
    public void Mapping_CoversEveryStandardEmuParamName_AndViceVersa()
    {
        var root = FindRepoRoot();
        var emuCsv = Path.Combine(root, "pointmaps", "models", "emu", "standard", "emu.csv");
        var mappingCsv = Path.Combine(root, "pointmaps", "models", "emu", "iec61850", "mapping.csv");
        Assert.True(File.Exists(emuCsv));
        Assert.True(File.Exists(mappingCsv));

        var emuParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(emuCsv).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var cols = line.Split(',');
            if (cols.Length >= 5)
                emuParams.Add(cols[4].Trim());
        }

        var mapping = Iec61850Mapping.Load(mappingCsv);
        var mapped = mapping.Entries.Select(e => e.ParamName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Empty(emuParams.Except(mapped, StringComparer.OrdinalIgnoreCase));
        Assert.Empty(mapped.Except(emuParams, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(emuParams.Count, mapping.Entries.Count);
    }

    [Fact]
    public void Icd_ContainsRequiredLogicalNodes()
    {
        var icd = File.ReadAllText(Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "iec61850", "pcs.icd"));
        foreach (var ln in new[] { "LLN0", "LPHD", "MMXU", "MMDC", "DRCC", "GGIO", "ZINV" })
            Assert.Contains(ln, icd, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_AcceptsRelativeAndAbsoluteRefs()
    {
        var mapping = Iec61850Mapping.Load(Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "iec61850", "mapping.csv"));
        const string ied = "TRNA_PCS01";
        Assert.True(mapping.TryResolve("PCS/GGIO1.SPCSO1", ied, out var yk3));
        Assert.Equal("yk3", yk3.ParamName);
        Assert.True(mapping.TryResolve("TRNA_PCS01PCS/MMXU1.TotW.mag.f", ied, out var p));
        Assert.Equal("yc27", p.ParamName);
        Assert.True(mapping.TryResolve("PCS/DRCC1.OutWSet.setMag.f", ied, out var yt0));
        Assert.Equal("yt0", yt0.ParamName);
    }

    [Fact]
    public void YkAndYt_AreGoose_YcAreMms()
    {
        var mapping = Iec61850Mapping.Load(Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "iec61850", "mapping.csv"));
        var goose = mapping.GooseEntries.Select(e => e.ParamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in new[] { "yk2", "yk3", "yt0", "yt1", "yt2", "yt3", "yt4" })
            Assert.Contains(name, goose);
        Assert.Equal(7, goose.Count);
        Assert.All(mapping.Entries.Where(e => e.ParamName.StartsWith("yc", StringComparison.OrdinalIgnoreCase)),
            e => Assert.False(e.IsGoose));
    }

    [Fact]
    public void Icd_ContainsGooseControl()
    {
        var icd = File.ReadAllText(Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "iec61850", "pcs.icd"));
        Assert.Contains("GoCB1", icd, StringComparison.Ordinal);
        Assert.Contains("dsGoose", icd, StringComparison.Ordinal);
        Assert.Contains("SPCSO2", icd, StringComparison.Ordinal);
        Assert.Contains("OutWSet", icd, StringComparison.Ordinal);
        Assert.DoesNotContain("<GOOSE max=\"0\"", icd, StringComparison.Ordinal);
    }

    [Fact]
    public void PointMapLoader_StillBindsStandardEmuCsv()
    {
        var path = Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", "standard", "emu.csv");
        var map = new ModbusPointMap(path, "simEmu1");
        Assert.Contains(map.ControlMaps, m => m.ParamName == "yk3");
        Assert.Contains(map.DataMaps, m => m.ParamName == "yc20");
    }
}

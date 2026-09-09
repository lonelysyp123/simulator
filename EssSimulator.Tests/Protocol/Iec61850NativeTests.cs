using EssSimulator.Protocol.Iec61850;

namespace EssSimulator.Tests.Protocol;

public class Iec61850NativeTests
{
    [Fact]
    public void CandidatePaths_IncludeNativeFileName()
    {
        var paths = Iec61850Native.ListCandidatePaths();
        Assert.NotEmpty(paths);
        string fileName = OperatingSystem.IsWindows() ? "iec61850.dll" : "libiec61850.dylib";
        Assert.Contains(paths, p => p.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(paths, p => p.Contains(Path.Combine("runtimes"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CandidatePaths_IncludeNativeDllSearchDirectories()
    {
        var extra = Path.Combine(Path.GetTempPath(), "ess-iec61850-native-test");
        Directory.CreateDirectory(extra);
        var previous = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
        try
        {
            AppContext.SetData("NATIVE_DLL_SEARCH_DIRECTORIES", extra);
            var paths = Iec61850Native.ListCandidatePaths();
            Assert.Contains(paths, p => Path.GetFullPath(p).StartsWith(Path.GetFullPath(extra), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            AppContext.SetData("NATIVE_DLL_SEARCH_DIRECTORIES", previous);
        }
    }

    [Fact]
    public void TryEnsureNativeLibrary_SucceedsInRepo()
    {
        Assert.True(Iec61850Native.TryEnsureNativeLibrary(out var detail), detail);
        Assert.True(File.Exists(detail), detail);
    }

    [Fact]
    public void CanUseRawEthernet_EmptyIfaceIsFalse()
    {
        Iec61850GooseEthernet.ResetCacheForTests();
        Assert.False(Iec61850GooseEthernet.CanUseRawEthernet(null, out var detail));
        Assert.Contains("未指定", detail, StringComparison.Ordinal);
        Iec61850GooseEthernet.ResetCacheForTests();
        Assert.False(Iec61850GooseEthernet.CanUseRawEthernet("", out _));
    }

    [Fact]
    public void HasGooseSubscriberDestroy_MatchesLoadedNativeLibrary()
    {
        Assert.True(Iec61850Native.TryEnsureNativeLibrary(out var path), path);
        Assert.True(Iec61850Native.HasGooseSubscriberDestroy(), path);
    }
}

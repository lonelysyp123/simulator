using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EssSimulator.Protocol.Iec61850
{
    /// <summary>
    /// 解析 libiec61850 原生库。C# 包装的 DllImport 名为 <c>iec61850</c>
    ///（macOS 找 <c>libiec61850.dylib</c>，Windows 找 <c>iec61850.dll</c>）。
    /// </summary>
    internal static class Iec61850Native
    {
        internal const string DllImportName = "iec61850";

        private static readonly object Gate = new();
        private static bool _registered;

        [ModuleInitializer]
        internal static void Init() => EnsureLoaded();

        internal static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (_registered)
                    return;

                var assembly = typeof(IEC61850.Server.IedServer).Assembly;
                NativeLibrary.SetDllImportResolver(assembly, Resolve);
                _registered = true;
            }
        }

        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!IsIec61850(libraryName))
                return IntPtr.Zero;

            foreach (var path in CandidatePaths())
            {
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                    return handle;
            }

            return IntPtr.Zero;
        }

        private static bool IsIec61850(string libraryName) =>
            libraryName.Equals(DllImportName, StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("libiec61850", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("libiec61850.dylib", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("iec61850.dll", StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<string> CandidatePaths()
        {
            string fileName = OperatingSystem.IsWindows() ? "iec61850.dll" : "libiec61850.dylib";
            var rids = HostRids();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var root in SearchRoots())
            {
                foreach (var rid in rids)
                {
                    TryAdd(seen, Path.Combine(root, "runtimes", rid, "native", fileName));
                    TryAdd(seen, Path.Combine(root, "lib61850", "native", rid, fileName));
                }

                TryAdd(seen, Path.Combine(root, fileName));
            }

            return seen;
        }

        private static IEnumerable<string> SearchRoots()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var start in new[]
                     {
                         AppContext.BaseDirectory,
                         Path.GetDirectoryName(typeof(Iec61850Native).Assembly.Location),
                         Directory.GetCurrentDirectory()
                     })
            {
                if (string.IsNullOrWhiteSpace(start))
                    continue;

                var dir = new DirectoryInfo(Path.GetFullPath(start));
                while (dir != null)
                {
                    if (seen.Add(dir.FullName))
                        yield return dir.FullName;
                    dir = dir.Parent;
                }
            }
        }

        private static IEnumerable<string> HostRids()
        {
            if (OperatingSystem.IsWindows())
            {
                yield return "win-x64";
                yield break;
            }

            if (OperatingSystem.IsMacOS())
            {
                if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                    yield return "osx-arm64";
                else
                    yield return "osx-x64";
                yield return "osx-universal";
                yield return RuntimeInformation.RuntimeIdentifier;
            }
        }

        private static void TryAdd(HashSet<string> seen, string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
                seen.Add(Path.GetFullPath(path));
        }
    }
}

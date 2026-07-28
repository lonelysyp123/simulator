using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EssSimulator.Licensing
{
    /// <summary>
    /// 跨平台机器码。算法须与 scripts/license/get-machine-id.* 保持一致：
    /// SHA256("EssSimulator|" + 原始机器标识小写) 的前 16 字节 → 32 位十六进制。
    /// </summary>
    public static class MachineIdProvider
    {
        public const string HashPrefix = "EssSimulator|";

        public static string GetMachineId()
        {
            string raw = GetRawMachineIdentity();
            return HashRaw(raw);
        }

        public static string HashRaw(string raw)
        {
            string normalized = (raw ?? "").Trim().ToLowerInvariant();
            byte[] bytes = Encoding.UTF8.GetBytes(HashPrefix + normalized);
            byte[] hash = SHA256.HashData(bytes);
            var sb = new StringBuilder(32);
            for (int i = 0; i < 16; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        public static string GetRawMachineIdentity()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsMachineGuid() ?? FallbackIdentity();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacPlatformUuid() ?? FallbackIdentity();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxMachineId() ?? FallbackIdentity();
            return FallbackIdentity();
        }

        [SupportedOSPlatform("windows")]
        private static string? GetWindowsMachineGuid()
        {
            try
            {
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid")?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string? GetMacPlatformUuid()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/sbin/ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return null;
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                // 行形如: "IOPlatformUUID" = "xxxxxxxx-xxxx-..."
                // 必须取等号右侧最后一个引号串，与 get-machine-id.sh 的 awk $4 一致
                var m = Regex.Match(
                    output,
                    @"IOPlatformUUID""\s*=\s*""([^""]+)""",
                    RegexOptions.CultureInvariant);
                if (m.Success)
                    return m.Groups[1].Value;
            }
            catch
            {
                /* ignore */
            }
            return null;
        }

        private static string? GetLinuxMachineId()
        {
            try
            {
                foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
                {
                    if (File.Exists(path))
                    {
                        string id = File.ReadAllText(path).Trim();
                        if (!string.IsNullOrEmpty(id))
                            return id;
                    }
                }
            }
            catch
            {
                /* ignore */
            }
            return null;
        }

        private static string FallbackIdentity()
        {
            return $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion}";
        }
    }
}

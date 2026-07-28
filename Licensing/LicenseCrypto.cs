using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace EssSimulator.Licensing
{
    public sealed class LicenseInfo
    {
        public string MachineId { get; init; } = "";
        public DateOnly Expires { get; init; }
        public string? Issued { get; init; }
    }

    public enum LicenseCheckStatus
    {
        Ok,
        FileMissing,
        InvalidFormat,
        BadSignature,
        MachineMismatch,
        Expired
    }

    public sealed class LicenseCheckResult
    {
        public LicenseCheckStatus Status { get; init; }
        public string Message { get; init; } = "";
        public LicenseInfo? Info { get; init; }
        public string LocalMachineId { get; init; } = "";
        public bool IsValid => Status == LicenseCheckStatus.Ok;
    }

    /// <summary>
    /// license.txt 格式（单行）：
    /// ESSLIC1.&lt;base64url(payloadJson)&gt;.&lt;base64url(hmacSha256)&gt;
    /// payload: {"machineId":"...","expires":"YYYY-MM-DD","issued":"YYYY-MM-DD"}
    /// </summary>
    public static class LicenseCrypto
    {
        public const string Prefix = "ESSLIC1";

        /// <summary>
        /// 与签发脚本共用的默认密钥。正式对外请用环境变量 ESS_LICENSE_SECRET 覆盖，
        /// 并同步修改私有签发脚本；勿把生产密钥提交到公开仓库。
        /// </summary>
        public const string DefaultSecret = "EssSimulator-License-Dev-ChangeMe-2026";

        public static string ResolveSecret()
        {
            string? env = Environment.GetEnvironmentVariable("ESS_LICENSE_SECRET");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();
            return DefaultSecret;
        }

        public static string Issue(string machineId, DateOnly expires, string? secret = null, DateOnly? issued = null)
        {
            string mid = (machineId ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(mid) || mid.Length != 32)
                throw new ArgumentException("machineId 应为 32 位十六进制机器码", nameof(machineId));

            var payload = new LicensePayload
            {
                MachineId = mid,
                Expires = expires.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Issued = (issued ?? DateOnly.FromDateTime(DateTime.UtcNow))
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            string json = System.Text.Json.JsonSerializer.Serialize(payload);
            string body = Base64UrlEncode(Encoding.UTF8.GetBytes(json));
            string sig = Base64UrlEncode(Hmac(secret ?? ResolveSecret(), body));
            return $"{Prefix}.{body}.{sig}";
        }

        public static bool TryParse(string text, string? secret, out LicenseInfo? info, out string error)
        {
            info = null;
            error = "";
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "授权文件为空";
                return false;
            }

            // 允许文件含注释行；取第一条 ESSLIC1. 开头的行
            string? line = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.Trim())
                .FirstOrDefault(l => !l.StartsWith('#') && l.StartsWith(Prefix + ".", StringComparison.Ordinal));

            if (line == null)
            {
                error = "未找到有效授权行（应以 ESSLIC1. 开头）";
                return false;
            }

            string[] parts = line.Split('.');
            if (parts.Length != 3 || parts[0] != Prefix)
            {
                error = "授权格式错误";
                return false;
            }

            string body = parts[1];
            string sig = parts[2];
            byte[] expected = Hmac(secret ?? ResolveSecret(), body);
            byte[] actual;
            try
            {
                actual = Base64UrlDecode(sig);
            }
            catch
            {
                error = "签名无法解析";
                return false;
            }

            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                error = "签名校验失败（密钥不匹配或文件被篡改）";
                return false;
            }

            try
            {
                string json = Encoding.UTF8.GetString(Base64UrlDecode(body));
                var payload = System.Text.Json.JsonSerializer.Deserialize<LicensePayload>(json);
                if (payload == null || string.IsNullOrWhiteSpace(payload.MachineId) || string.IsNullOrWhiteSpace(payload.Expires))
                {
                    error = "授权载荷不完整";
                    return false;
                }
                if (!DateOnly.TryParseExact(payload.Expires, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var expires))
                {
                    error = "到期日格式错误";
                    return false;
                }

                info = new LicenseInfo
                {
                    MachineId = payload.MachineId.Trim().ToLowerInvariant(),
                    Expires = expires,
                    Issued = payload.Issued
                };
                return true;
            }
            catch (Exception ex)
            {
                error = "授权载荷解析失败: " + ex.Message;
                return false;
            }
        }

        private static byte[] Hmac(string secret, string body)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            byte[] data = Encoding.UTF8.GetBytes(body);
            return HMACSHA256.HashData(key, data);
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string s)
        {
            string t = s.Replace('-', '+').Replace('_', '/');
            switch (t.Length % 4)
            {
                case 2: t += "=="; break;
                case 3: t += "="; break;
            }
            return Convert.FromBase64String(t);
        }

        private sealed class LicensePayload
        {
            public string MachineId { get; set; } = "";
            public string Expires { get; set; } = "";
            public string? Issued { get; set; }
        }
    }

    public static class LicenseGuard
    {
        public static LicenseCheckResult ValidateFile(string licensePath, string? secret = null, DateOnly? today = null, string? localMachineId = null)
        {
            string localId = string.IsNullOrWhiteSpace(localMachineId)
                ? MachineIdProvider.GetMachineId()
                : localMachineId.Trim().ToLowerInvariant();
            if (!File.Exists(licensePath))
            {
                return new LicenseCheckResult
                {
                    Status = LicenseCheckStatus.FileMissing,
                    LocalMachineId = localId,
                    Message = $"未找到授权文件: {licensePath}"
                };
            }

            string text = File.ReadAllText(licensePath);
            return ValidateContent(text, localId, secret, today);
        }

        public static LicenseCheckResult ValidateContent(string text, string localMachineId, string? secret = null, DateOnly? today = null)
        {
            string localId = (localMachineId ?? "").Trim().ToLowerInvariant();
            if (!LicenseCrypto.TryParse(text, secret, out var info, out var error) || info == null)
            {
                return new LicenseCheckResult
                {
                    Status = error.Contains("签名", StringComparison.Ordinal) ? LicenseCheckStatus.BadSignature : LicenseCheckStatus.InvalidFormat,
                    LocalMachineId = localId,
                    Message = error
                };
            }

            if (!string.Equals(info.MachineId, localId, StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseCheckResult
                {
                    Status = LicenseCheckStatus.MachineMismatch,
                    LocalMachineId = localId,
                    Info = info,
                    Message = $"机器码不匹配。本机={localId}，授权={info.MachineId}"
                };
            }

            var now = today ?? DateOnly.FromDateTime(DateTime.Now);
            if (info.Expires < now)
            {
                return new LicenseCheckResult
                {
                    Status = LicenseCheckStatus.Expired,
                    LocalMachineId = localId,
                    Info = info,
                    Message = $"授权已过期（到期日 {info.Expires:yyyy-MM-dd}，今天 {now:yyyy-MM-dd}）"
                };
            }

            return new LicenseCheckResult
            {
                Status = LicenseCheckStatus.Ok,
                LocalMachineId = localId,
                Info = info,
                Message = $"授权有效，到期日 {info.Expires:yyyy-MM-dd}"
            };
        }
    }
}

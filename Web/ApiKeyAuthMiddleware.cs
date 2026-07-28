using System.Security.Cryptography;
using System.Text;
using EssSimulator.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EssSimulator.Web
{
    /// <summary>
    /// 可选 API Key 鉴权：仅当 <see cref="WebConfig.ApiKeyEnabled"/> 为 true 时生效。
    /// 保护 <c>/api/*</c>（<c>/api/health</c> 豁免，供探活）；不拦截静态页与 SignalR。
    /// </summary>
    public sealed class ApiKeyAuthMiddleware
    {
        public const string HeaderName = "X-Api-Key";

        private readonly RequestDelegate _next;

        public ApiKeyAuthMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IOptions<WebConfig> webOptions)
        {
            var path = context.Request.Path;
            if (!path.StartsWithSegments("/api") || IsExemptPath(path))
            {
                await _next(context);
                return;
            }

            var cfg = webOptions.Value;
            if (!TryAuthorize(cfg, GetProvidedKey(context.Request), out var status, out var message))
            {
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { message });
                return;
            }

            await _next(context);
        }

        public static bool IsExemptPath(PathString path) =>
            path.StartsWithSegments("/api/health");

        public static string? GetProvidedKey(HttpRequest request)
        {
            if (request.Headers.TryGetValue(HeaderName, out var header) && !string.IsNullOrWhiteSpace(header))
                return header.ToString().Trim();

            var auth = request.Headers.Authorization.ToString();
            if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return auth["Bearer ".Length..].Trim();
            if (auth.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                return auth["ApiKey ".Length..].Trim();

            return null;
        }

        /// <summary>返回 true 表示放行。</summary>
        public static bool TryAuthorize(WebConfig cfg, string? providedKey, out int statusCode, out string message)
        {
            statusCode = StatusCodes.Status200OK;
            message = "";

            if (cfg is null || !cfg.ApiKeyEnabled)
                return true;

            if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            {
                statusCode = StatusCodes.Status503ServiceUnavailable;
                message = "API Key 已启用但未配置（请设置 Simulator:Web:ApiKey 或环境变量 Simulator__Web__ApiKey）";
                return false;
            }

            if (string.IsNullOrWhiteSpace(providedKey) || !FixedEquals(cfg.ApiKey, providedKey))
            {
                statusCode = StatusCodes.Status401Unauthorized;
                message = $"缺少或错误的 API Key（请通过请求头 {HeaderName} 或 Authorization: Bearer 提供）";
                return false;
            }

            return true;
        }

        private static bool FixedEquals(string expected, string actual)
        {
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(actual);
            if (a.Length != b.Length)
                return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}

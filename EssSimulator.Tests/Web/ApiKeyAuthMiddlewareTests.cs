using EssSimulator.Configuration;
using EssSimulator.Web;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EssSimulator.Tests.Web;

public class ApiKeyAuthMiddlewareTests
{
    [Fact]
    public void TryAuthorize_disabled_allows_without_key()
    {
        var cfg = new WebConfig { ApiKeyEnabled = false, ApiKey = "secret" };
        Assert.True(ApiKeyAuthMiddleware.TryAuthorize(cfg, null, out var status, out _));
        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Fact]
    public void TryAuthorize_enabled_without_configured_key_returns_503()
    {
        var cfg = new WebConfig { ApiKeyEnabled = true, ApiKey = "" };
        Assert.False(ApiKeyAuthMiddleware.TryAuthorize(cfg, "any", out var status, out var message));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Contains("未配置", message);
    }

    [Fact]
    public void TryAuthorize_enabled_rejects_missing_or_wrong_key()
    {
        var cfg = new WebConfig { ApiKeyEnabled = true, ApiKey = "correct" };
        Assert.False(ApiKeyAuthMiddleware.TryAuthorize(cfg, null, out var s1, out _));
        Assert.Equal(StatusCodes.Status401Unauthorized, s1);
        Assert.False(ApiKeyAuthMiddleware.TryAuthorize(cfg, "wrong", out var s2, out _));
        Assert.Equal(StatusCodes.Status401Unauthorized, s2);
    }

    [Fact]
    public void TryAuthorize_enabled_accepts_matching_key()
    {
        var cfg = new WebConfig { ApiKeyEnabled = true, ApiKey = "correct" };
        Assert.True(ApiKeyAuthMiddleware.TryAuthorize(cfg, "correct", out var status, out _));
        Assert.Equal(StatusCodes.Status200OK, status);
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/health/")]
    public void IsExemptPath_health(string path)
    {
        Assert.True(ApiKeyAuthMiddleware.IsExemptPath(path));
    }

    [Theory]
    [InlineData("/api/command")]
    [InlineData("/api/link/em/off")]
    [InlineData("/api/config")]
    public void IsExemptPath_non_health(string path)
    {
        Assert.False(ApiKeyAuthMiddleware.IsExemptPath(path));
    }
}

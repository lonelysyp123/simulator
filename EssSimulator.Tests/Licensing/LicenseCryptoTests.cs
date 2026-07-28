using System.Globalization;
using EssSimulator.Licensing;
using Xunit;

namespace EssSimulator.Tests.Licensing;

public class LicenseCryptoTests
{
    private const string Secret = "unit-test-secret";
    private const string Machine = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void Issue_and_parse_roundtrip()
    {
        var expires = new DateOnly(2030, 1, 15);
        string token = LicenseCrypto.Issue(Machine, expires, Secret, new DateOnly(2026, 1, 1));
        Assert.StartsWith("ESSLIC1.", token);
        Assert.True(LicenseCrypto.TryParse(token, Secret, out var info, out var err), err);
        Assert.NotNull(info);
        Assert.Equal(Machine, info!.MachineId);
        Assert.Equal(expires, info.Expires);
    }

    [Fact]
    public void Validate_ok_when_machine_and_date_match()
    {
        string token = LicenseCrypto.Issue(Machine, new DateOnly(2030, 6, 1), Secret);
        var r = LicenseGuard.ValidateContent(token, Machine, Secret, new DateOnly(2026, 7, 28));
        Assert.True(r.IsValid, r.Message);
        Assert.Equal(LicenseCheckStatus.Ok, r.Status);
    }

    [Fact]
    public void Validate_rejects_wrong_machine()
    {
        string token = LicenseCrypto.Issue(Machine, new DateOnly(2030, 6, 1), Secret);
        var r = LicenseGuard.ValidateContent(token, "ffffffffffffffffffffffffffffffff", Secret, new DateOnly(2026, 7, 28));
        Assert.False(r.IsValid);
        Assert.Equal(LicenseCheckStatus.MachineMismatch, r.Status);
    }

    [Fact]
    public void Validate_rejects_expired()
    {
        string token = LicenseCrypto.Issue(Machine, new DateOnly(2025, 1, 1), Secret);
        var r = LicenseGuard.ValidateContent(token, Machine, Secret, new DateOnly(2026, 7, 28));
        Assert.False(r.IsValid);
        Assert.Equal(LicenseCheckStatus.Expired, r.Status);
    }

    [Fact]
    public void Validate_allows_on_expiry_day()
    {
        var day = new DateOnly(2027, 7, 28);
        string token = LicenseCrypto.Issue(Machine, day, Secret);
        var r = LicenseGuard.ValidateContent(token, Machine, Secret, day);
        Assert.True(r.IsValid, r.Message);
    }

    [Fact]
    public void HashRaw_is_stable()
    {
        string a = MachineIdProvider.HashRaw("ABC");
        string b = MachineIdProvider.HashRaw("abc");
        string c = MachineIdProvider.HashRaw(" abc ");
        Assert.Equal(32, a.Length);
        Assert.Equal(a, b);
        Assert.Equal(a, c);
    }

    [Fact]
    public void HashRaw_matches_python_script_vector()
    {
        // 与 scripts/license/get-machine-id.sh 中 python3 算法一致
        Assert.Equal(
            "1cee3852156caa4c9567b38c4be0c217",
            MachineIdProvider.HashRaw("C7A89A38-4668-5A96-AAD9-75CCF0814319"));
    }
}

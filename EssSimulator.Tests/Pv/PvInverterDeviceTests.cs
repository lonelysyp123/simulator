using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Pv;

namespace EssSimulator.Tests.Pv;

public class PvInverterDeviceTests
{
    private static PvInverterDevice CreateRunning()
    {
        var inv = PvInverterDevice.Create320kW("pv_inv1");
        inv.SyncExternalRunCommand(true);
        inv.TransitionToMode(OperationMode.Normal);
        inv.UpdateGridState(690, 50, isUtilityGridAvailable: true);
        return inv;
    }

    [Fact]
    public void Factory_WiresThirtyModulesTimesSixteenStrings()
    {
        var inv = PvInverterDevice.Create320kW("pv_inv1");
        Assert.Equal(16, inv.StringCount);
        Assert.Equal(30, inv.ModulesPerString);
        Assert.Equal(320, inv.RatedPowerKw);
        Assert.Equal(16 * 30, inv.TotalModuleCount);
        Assert.Equal(690, new PvInverterConfig().AcNominalLineVoltageV);
        Assert.Equal(50, new PvInverterConfig().FrequencyHz);
        Assert.Equal(0.01, new PvInverterConfig().GridLossCoefficient);
    }

    [Fact]
    public void Stc_ClipsToRatedAcPower()
    {
        var inv = CreateRunning();
        inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var st = inv.GetCurrentState();
        Assert.InRange(st.ActivePower, 318, 322);
        Assert.True(st.ActivePower >= 0);
        Assert.InRange(st.DcVoltage, 1100, 1400);
        Assert.Equal(16, inv.StringCurrentsA.Count);
        Assert.True(inv.AvailableDcPowerKw > st.ActivePower);
    }

    [Fact]
    public void SetPowerCommand_RejectsChargeAndCurtails()
    {
        var inv = CreateRunning();
        inv.SetPowerCommand(-200, 0);
        for (int i = 0; i < 40; i++)
            inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));

        Assert.InRange(inv.GetCurrentState().ActivePower, -0.05, 0.05);

        inv.SetPowerCommand(160, 0);
        for (int i = 0; i < 40; i++)
            inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));

        Assert.InRange(inv.GetCurrentState().ActivePower, 158, 162);
        Assert.True(inv.GetCurrentState().ActivePower >= 0);
    }

    [Fact]
    public void NightOrOff_OutputsZeroActivePower()
    {
        var inv = CreateRunning();
        inv.Update(0, 25, DateTime.UtcNow, TimeSpan.FromSeconds(1));
        Assert.Equal(0, inv.GetCurrentState().ActivePower);

        inv = CreateRunning();
        inv.SyncExternalRunCommand(false);
        inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(1));
        Assert.Equal(OperationMode.Off, inv.GetCurrentState().Mode);
        Assert.Equal(0, inv.GetCurrentState().ActivePower);
    }

    [Fact]
    public void HalfIrradiance_BelowRated_FollowsArray()
    {
        var inv = CreateRunning();
        inv.Update(500, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        var st = inv.GetCurrentState();
        Assert.True(st.ActivePower < 250);
        Assert.True(st.ActivePower > 100);
        Assert.True(st.ActivePower >= 0);
    }

    [Fact]
    public void ExtremeCold_DeratesBelowRatedUnlikeMildCold()
    {
        var inv = CreateRunning();
        inv.Update(1000, 0, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        double mildCold = inv.GetCurrentState().ActivePower;
        Assert.InRange(mildCold, 318, 322);

        inv = CreateRunning();
        inv.Update(1000, -40, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.True(inv.GetCurrentState().ActivePower < 50);
        Assert.True(inv.AvailableDcPowerKw < 50);
    }

    [Fact]
    public void ColdDerate_AtMinus25_IsAboutHalfOfFullAvailable()
    {
        var inv = CreateRunning();
        inv.Update(1000, -25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.InRange(inv.GetCurrentState().ActivePower, 140, 230);
    }

    [Fact]
    public void LimitReason_ReportsOffIrradianceSetpointRatedAndCold()
    {
        var inv = PvInverterDevice.Create320kW("pv_inv1");
        inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(1));
        Assert.Equal("停机", inv.LimitReason);

        inv = CreateRunning();
        inv.Update(0, 25, DateTime.UtcNow, TimeSpan.FromSeconds(1));
        Assert.Equal("辐照不足", inv.LimitReason);

        inv = CreateRunning();
        inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.Equal("已达额定", inv.LimitReason);
        Assert.InRange(inv.GetCurrentState().DcVoltage, 1100, 1400);
        Assert.True(inv.GetCurrentState().DcCurrent > 1);

        inv = CreateRunning();
        inv.SetPowerCommand(160, 0);
        for (int i = 0; i < 40; i++)
            inv.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));
        Assert.Equal("有功设定", inv.LimitReason);

        inv = CreateRunning();
        inv.Update(1000, -25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.Equal("低温降额", inv.LimitReason);
    }
}

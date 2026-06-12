using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Devices;

public class LoadDeviceTests
{
    [Fact]
    public void SetPowered_ForcesZeroPower()
    {
        var load = new LoadDevice("load_35", 100, 20);
        load.RefreshSchedule(DateTime.UtcNow);

        load.SetPowered(false);

        Assert.Equal(0, load.ActivePower);
        Assert.Equal(0, load.ReactivePower);
    }

    [Fact]
    public void SetLoadCharacteristic_OverridesSchedule()
    {
        var load = new LoadDevice("load_35", 100, 0);
        load.SetLoadCharacteristic("activePower", 42);
        load.RefreshSchedule(DateTime.UtcNow);

        Assert.Equal(42, load.ActivePower);
    }

    [Fact]
    public void RefreshSchedule_PicksLatestWindow()
    {
        var windows = new[]
        {
            new LoadWindow { Start = TimeSpan.Zero, ActivePowerPlan = 10, ReactivePowerPlan = 1 },
            new LoadWindow { Start = TimeSpan.FromHours(12), ActivePowerPlan = 50, ReactivePowerPlan = 5 }
        };
        var load = new LoadDevice("load_35", 10, 1, windows);
        var noon = new DateTime(2026, 1, 1, 13, 0, 0);

        load.RefreshSchedule(noon);

        Assert.InRange(load.ActivePower, 49.9, 50.1);
        Assert.Equal(5, load.ReactivePower);
    }

    [Fact]
    public void ComputeLoadCurrentA_NegativeWhenConsuming()
    {
        var load = new LoadDevice("load_35", -100, 0);
        load.RefreshSchedule(DateTime.UtcNow);

        double current = load.ComputeLoadCurrentA(35_000, DateTime.UtcNow);

        Assert.True(current > 0);
    }
}

using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Pv;

namespace EssSimulator.Tests.Pv;

public class PvUnitDeviceTests
{
    [Fact]
    public void Default_HasSixteenInvertersEachUnchanged()
    {
        var unit = PvUnitDevice.CreateDefault("pv_unit1");
        Assert.Equal(16, unit.InverterCount);
        Assert.Equal(5120, unit.RatedPowerKw);
        Assert.Equal(16 * 16 * 30, unit.TotalModuleCount);
        Assert.All(unit.Inverters, inv =>
        {
            Assert.Equal(16, inv.StringCount);
            Assert.Equal(30, inv.ModulesPerString);
            Assert.Equal(320, inv.RatedPowerKw);
        });
        Assert.Equal(35000, new PvUnitConfig().UnitXfPrimaryV);
        Assert.Equal(690, new PvUnitConfig().UnitXfSecondaryV);
        Assert.Equal(5120, new PvUnitConfig().UnitXfRatedKva);
    }

    [Fact]
    public void Stc_ClipsToSixteenTimesInverterRatedAc()
    {
        var unit = PvUnitDevice.CreateDefault("pv_unit1");
        unit.SyncExternalRunCommand(true);
        unit.TransitionToMode(OperationMode.Normal);
        unit.UpdateGridState(690, 50, isUtilityGridAvailable: true);

        unit.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.InRange(unit.ActivePowerKw, 16 * 318, 16 * 322);
        Assert.True(unit.ActivePowerKw >= 0);
        Assert.True(unit.AvailableDcPowerKw > unit.ActivePowerKw);
    }

    [Fact]
    public void Station_ExposesPointTableSubsystems()
    {
        var unit = PvUnitDevice.CreateDefault("pv_unit1");
        Assert.NotNull(unit.Logger);
        Assert.NotNull(unit.Transformer);
        Assert.NotNull(unit.MeterLv);
        Assert.NotNull(unit.MeterHv);
        Assert.NotNull(unit.MvIo1);
        Assert.NotNull(unit.MvIo2);
        Assert.NotNull(unit.Pid1);
        Assert.NotNull(unit.Pid2);
        Assert.NotNull(unit.Weather);
        Assert.Equal(25, unit.Logger.ConnectedDeviceCount);
    }

    [Fact]
    public void Stc_LoggerAndMetersFollowInverterAc()
    {
        var unit = StartDefault();
        unit.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));

        Assert.Equal(1, unit.Logger.RunState);
        Assert.Equal(16, unit.Logger.GridConnectedDeviceCount);
        Assert.Equal(0, unit.Logger.OffGridDeviceCount);
        Assert.InRange(unit.Logger.TotalActivePowerW, 16 * 318e3, 16 * 322e3);
        Assert.InRange(unit.Logger.ApparentPowerVa, unit.Logger.TotalActivePowerW, unit.Logger.TotalActivePowerW * 1.05);
        Assert.Equal(unit.RatedPowerKw, unit.Logger.NominalActivePowerKw, 3);
        Assert.Equal(0, unit.Logger.MinAdjustableActivePowerKw, 3);
        Assert.Equal(unit.RatedPowerKw, unit.Logger.MaxAdjustableActivePowerKw, 3);

        Assert.InRange(unit.MeterLv.LineVoltageAb, 680, 700);
        Assert.InRange(unit.MeterHv.LineVoltageAb, 34000, 36000);
        Assert.Equal(unit.MeterLv.TotalActivePowerW, unit.Logger.TotalActivePowerW, 0);
        Assert.True(unit.MeterLv.FeedInPowerM1W > 0);
        Assert.True(unit.MeterHv.PhaseACurrent > 0);
        Assert.True(unit.MeterHv.PhaseACurrent < unit.MeterLv.PhaseACurrent);
    }

    [Fact]
    public void Weather_PublishesIrradianceAndModuleTemp()
    {
        var unit = StartDefault();
        unit.Update(800, 20, DateTime.UtcNow, TimeSpan.FromHours(1));

        Assert.Equal(800, unit.Weather.SlopeIrradianceWm2, 3);
        Assert.InRange(unit.Weather.HorizontalIrradianceWm2, 700, 800);
        Assert.Equal(20, unit.Weather.ModuleTemperatureC, 3);
        Assert.InRange(unit.Weather.DailySlopeIrradiationWhm2, 790, 810);
    }

    [Fact]
    public void Transformer_OilExceedsAmbientUnderLoad()
    {
        var unit = StartDefault();
        unit.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.True(unit.Transformer.OilTemperatureC > 25);
        Assert.True(unit.Transformer.WindingTemperatureC > unit.Transformer.OilTemperatureC);
        Assert.False(unit.Logger.OilTemperatureAlarm);
        Assert.Equal(unit.Transformer.OilTemperatureC, unit.Logger.Pt100_1C, 3);
        Assert.Equal(unit.Transformer.WindingTemperatureC, unit.Logger.Pt100_2C, 3);
    }

    [Fact]
    public void SubarrayActivePercent_CurtailsAllInverters()
    {
        var unit = StartDefault();
        unit.Logger.SubarrayActivePowerPercent = 50;
        for (int i = 0; i < 40; i++)
            unit.Update(1000, 25, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));

        Assert.InRange(unit.ActivePowerKw, 16 * 158, 16 * 162);
        Assert.InRange(unit.Logger.TotalActivePowerW, 16 * 158e3, 16 * 162e3);
    }

    [Fact]
    public void Energy_AccumulatesOnLoggerAndExportMeters()
    {
        var unit = StartDefault();
        unit.Update(1000, 25, new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc), TimeSpan.FromHours(1));
        Assert.InRange(unit.Logger.DailyYieldKwh, 16 * 318, 16 * 322);
        Assert.InRange(unit.MeterLv.ReverseActiveEnergyKwh, 16 * 318, 16 * 322);
        Assert.Equal(0, unit.MeterLv.ForwardActiveEnergyKwh, 3);
    }

    [Fact]
    public void ArrayClimate_AngleAndTemperature_ScaleAvailablePowerIndependently()
    {
        var unit = StartDefault();
        unit.ArrayA.SetAmbientTemperatureC(25);
        unit.ArrayA.SetIncidenceAngleDeg(90);
        unit.ArrayB.SetAmbientTemperatureC(25);
        unit.ArrayB.SetIncidenceAngleDeg(90);
        unit.Update(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        double bothFull = unit.MaximumDischargePowerKw;

        unit.ArrayB.SetIncidenceAngleDeg(20);
        unit.Update(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.True(unit.ArrayA.AvailableAcPowerKw > unit.ArrayB.AvailableAcPowerKw * 2);
        Assert.True(unit.ArrayA.ActivePowerKw > unit.ArrayB.ActivePowerKw * 2);
        Assert.Equal("已达额定", unit.ArrayA.LimitReason);
        Assert.True(unit.ArrayA.DcVoltageV > 1000);
        Assert.True(unit.ArrayA.DcCurrentA > unit.ArrayB.DcCurrentA);
        Assert.True(unit.MaximumDischargePowerKw < bothFull * 0.85);
        Assert.True(unit.MaximumDischargePowerKw > bothFull * 0.4);

        double afterAngle = unit.MaximumDischargePowerKw;
        unit.ArrayA.SetAmbientTemperatureC(70);
        unit.Update(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        Assert.True(unit.MaximumDischargePowerKw < afterAngle);
        Assert.True(unit.ActivePowerKw <= unit.MaximumDischargePowerKw + 1);
    }

    private static PvUnitDevice StartDefault()
    {
        var unit = PvUnitDevice.CreateDefault("pv_unit1");
        unit.SyncExternalRunCommand(true);
        unit.TransitionToMode(OperationMode.Normal);
        unit.UpdateGridState(690, 50, isUtilityGridAvailable: true);
        return unit;
    }
}

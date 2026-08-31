using EssSimulator.EssDeviceSimModel.Pv;
using EssSimulator.EssSimModelApi;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.Pv;

/// <summary>
/// 核对 pv_apm810.csv / pv_logger.csv 的 ModelSim 是否绑到光伏单元已有属性。
/// </summary>
public class PvPointMapBindingTests
{
    private static readonly string[] Apm810Expected =
    {
        "yc0:MeterLv.PhaseAVoltage",
        "yc1:MeterLv.PhaseBVoltage",
        "yc2:MeterLv.PhaseCVoltage",
        "yc3:MeterLv.LineVoltageAb",
        "yc4:MeterLv.LineVoltageBc",
        "yc5:MeterLv.LineVoltageCa",
        "yc6:MeterLv.PhaseACurrent",
        "yc7:MeterLv.PhaseBCurrent",
        "yc8:MeterLv.PhaseCCurrent",
        "yc9:MeterLv.PhaseAActivePowerW",
        "yc10:MeterLv.PhaseBActivePowerW",
        "yc11:MeterLv.PhaseCActivePowerW",
        "yc12:MeterLv.TotalActivePowerW",
        "yc13:MeterLv.FeedInPowerM1W",
        "yc14:MeterLv.PhaseAReactivePowerVar",
        "yc15:MeterLv.PhaseBReactivePowerVar",
        "yc16:MeterLv.PhaseCReactivePowerVar",
        "yc17:MeterLv.TotalReactivePowerVar",
        "yc18:MeterLv.PhaseAApparentPowerVa",
        "yc19:MeterLv.PhaseBApparentPowerVa",
        "yc20:MeterLv.PhaseCApparentPowerVa",
        "yc21:MeterLv.TotalApparentPowerVa",
        "yc22:MeterLv.TotalPowerFactor",
        "yc23:MeterLv.FrequencyHz",
        "yc24:MeterLv.ForwardActiveEnergyKwh",
        "yc25:MeterLv.ReverseActiveEnergyKwh",
        "yc26:MeterLv.ForwardReactiveEnergyKvarh",
        "yc27:MeterLv.ReverseReactiveEnergyKvarh",
        "yc30:MeterLv.FeedInPowerM2W",
        "yc31:MeterLv.PhaseAPowerFactor",
        "yc32:MeterLv.PhaseBPowerFactor",
        "yc33:MeterLv.PhaseCPowerFactor"
    };

    private static readonly string[] LoggerExpected =
    {
        "yc0:Logger.DigitalInputBitmap",
        "yx7:Logger.OilTemperatureAlarm",
        "yx8:Logger.OilTemperatureTrip",
        "yx11:Logger.WindingTemperatureAlarm",
        "yx12:Logger.WindingTemperatureTrip",
        "yc1:Logger.Pt100_1C",
        "yc2:Logger.Pt100_2C",
        "yc3:Logger.Adc1Voltage",
        "yc4:Logger.Adc1CurrentMa",
        "yc5:Logger.Adc2Voltage",
        "yc6:Logger.Adc2CurrentMa",
        "yc7:Logger.Adc3Voltage",
        "yc8:Logger.Adc4Voltage",
        "yc9:Logger.Adc3CurrentMa",
        "yc10:Logger.Adc4CurrentMa",
        "yc11:Logger.ConnectedDeviceCount",
        "yc12:Logger.FaultDeviceCount",
        "yc13:Logger.RunState",
        "yc14:Logger.UnlatchState",
        "yc15:Logger.TotalActivePowerW",
        "yc16:Logger.DailyYieldKwh",
        "yc17:Logger.TotalReactivePowerVar",
        "yc18:Logger.TotalYieldKwh",
        "yc19:Logger.MinAdjustableActivePowerKw",
        "yc20:Logger.MaxAdjustableActivePowerKw",
        "yc21:Logger.MinAdjustableReactivePowerKvar",
        "yc22:Logger.MaxAdjustableReactivePowerKvar",
        "yc23:Logger.NominalActivePowerKw",
        "yc24:Logger.NominalReactivePowerKvar",
        "yc25:Logger.GridConnectedDeviceCount",
        "yc26:Logger.OffGridDeviceCount",
        "yc27:Logger.MonthlyYieldKwh",
        "yc28:Logger.AnnualYieldKwh",
        "yc29:Logger.ApparentPowerVa",
        "yt0:Logger.DoRmuTrip",
        "yt1:Logger.DoLvFan",
        "yt2:Logger.Do3",
        "yt3:Logger.Do4",
        "yt4:Logger.SubarrayOnOff",
        "yt5:Logger.SubarrayActivePowerKw",
        "yt6:Logger.SubarrayActivePowerPercent",
        "yt7:Logger.SubarrayReactivePowerKvar",
        "yt8:Logger.SubarrayReactivePowerPercent",
        "yt9:Logger.SubarrayPowerFactor"
    };

    private static readonly string[] LoggerMissingDin =
    {
        "yx0", "yx1", "yx2", "yx3", "yx4", "yx5", "yx6",
        "yx9", "yx10", "yx13", "yx14", "yx15"
    };

    [Fact]
    public void ObjectPathResolver_ReadsEveryBoundMeterAndLoggerPath()
    {
        var unit = PvUnitDevice.CreateDefault("pv1");
        foreach (var spec in Apm810Expected.Concat(LoggerExpected))
        {
            var path = spec.Split(':')[1];
            Assert.False(ObjectPathResolver.GetValue(unit, path) is null, path);
        }
    }

    [Fact]
    public void Apm810Csv_BindsKnownPoints_LeavesReservedUnbound()
    {
        var map = LoadMap("pv_apm810.csv");
        AssertBindings(map, Apm810Expected);
        Assert.False(map.ParamModelLookup.ContainsKey("yc28"));
        Assert.False(map.ParamModelLookup.ContainsKey("yc29"));
        Assert.Equal(Apm810Expected.Length, map.ParamModelLookup.Count);
    }

    [Fact]
    public void LoggerCsv_BindsKnownPoints_LeavesMissingDinUnbound()
    {
        var map = LoadMap("pv_logger.csv");
        AssertBindings(map, LoggerExpected);
        foreach (var param in LoggerMissingDin)
            Assert.False(map.ParamModelLookup.ContainsKey(param), param);
        Assert.Equal(LoggerExpected.Length, map.ParamModelLookup.Count);
    }

    private static void AssertBindings(ModbusPointMap map, string[] expected)
    {
        foreach (var spec in expected)
        {
            var parts = spec.Split(':');
            Assert.True(map.ParamModelLookup.TryGetValue(parts[0], out var model), parts[0]);
            Assert.Equal($"pv1.{parts[1]}", model.Arg1);
        }
    }

    private static ModbusPointMap LoadMap(string fileName)
    {
        var path = Path.Combine(FindRepoRoot(), "pointmaps", "models", "pv", "standard", fileName);
        Assert.True(File.Exists(path), path);
        return new ModbusPointMap(path, "simPv1");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "EssSimulator.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("未找到仓库根目录");
    }
}

using EssSimulator.DataExchange.Adapters;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.DataExchange.Pipeline;
using EssSimulator.DataExchange.Plugins;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

/// <summary>EMU 故障字设备插件层：组字规则、注册表、目录加载与遥测管线接入。</summary>
public class TelemetryPluginTests
{
    private const string Root = "emu1.PcsList[0]";

    [Fact]
    public void Compute_WarningWord1_CombinesSupportedFaults()
    {
        var sim = new FakeSimulation
        {
            [$"{Root}.InsulationAlarm"] = true,
            [$"{Root}.DcSurgeProtectorAbnormal"] = true
        };
        var plugin = new TrinaEmuFaultWordPlugin();

        // Bit0/Bit1 总线绝缘低（同源 InsulationAlarm）+ Bit10 DC SPD
        Assert.Equal((1 << 0) | (1 << 1) | (1 << 10), plugin.Compute("ModuleWarningWord1", Root, sim));
    }

    [Fact]
    public void Compute_WarningWord1_DriveAndSpdBits()
    {
        var sim = new FakeSimulation
        {
            [$"{Root}.InternalOverTemp"] = true,
            [$"{Root}.DriveFault"] = true,
            [$"{Root}.AcSurgeProtectorAbnormal"] = true
        };
        var plugin = new TrinaEmuFaultWordPlugin();

        // Bit5 J板过温 + Bit6/7/8 驱动线松动 + Bit12 AC SPD
        Assert.Equal((1 << 5) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 12),
            plugin.Compute("ModuleWarningWord1", Root, sim));
    }

    [Fact]
    public void Compute_WarningWord1_NoFaultsAndUnsupportedBits_ReturnsZero()
    {
        var sim = new FakeSimulation();
        var plugin = new TrinaEmuFaultWordPlugin();

        Assert.Equal(0, plugin.Compute("ModuleWarningWord1", Root, sim));

        // 置位无协议位对应的仿真故障（湿度等位仿真不支持），字值仍为 0
        sim[$"{Root}.GridOverVoltage"] = true;
        Assert.Equal(0, plugin.Compute("ModuleWarningWord1", Root, sim));
    }

    [Fact]
    public void Compute_WarningWord2_CbcAndFanBits()
    {
        var plugin = new TrinaEmuFaultWordPlugin();

        var sim = new FakeSimulation { [$"{Root}.InverterSoftwareOverCurrent"] = true };
        Assert.Equal(0b00111, plugin.Compute("ModuleWarningWord2", Root, sim)); // Bit0/1/2 CBC

        sim = new FakeSimulation { [$"{Root}.AcFanAbnormal"] = true };
        Assert.Equal(1 << 4, plugin.Compute("ModuleWarningWord2", Root, sim));  // Bit4 风扇欠流
    }

    [Fact]
    public void Compute_MissingPath_TreatedAsZero()
    {
        // deviceRoot 指向不存在的设备路径：全部按 0 处理，不抛异常
        var plugin = new TrinaEmuFaultWordPlugin();
        Assert.Equal(0, plugin.Compute("ModuleWarningWord1", "emu9.PcsList[0]", new FakeSimulation()));
    }

    [Fact]
    public void CanHandle_UnknownWordKey_ReturnsNull()
    {
        var plugin = new TrinaEmuFaultWordPlugin();
        Assert.False(plugin.CanHandle("SystemFaultWord1"));
        Assert.Null(plugin.Compute("SystemFaultWord1", Root, new FakeSimulation()));
    }

    [Fact]
    public void Compute_SystemFaultSummary_AnyModuleAlarm_ReturnsOne()
    {
        var sim = new FakeSimulation
        {
            ["emu1.PcsList.Count"] = 2,
            ["emu1.PcsList[0].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[0].AlarmSummary2"] = (ushort)0,
            ["emu1.PcsList[1].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[1].AlarmSummary2"] = (ushort)0,
            ["emu2.PcsList.Count"] = 2,
            ["emu2.PcsList[0].AlarmSummary1"] = (ushort)0,
            ["emu2.PcsList[0].AlarmSummary2"] = (ushort)0,
            ["emu2.PcsList[1].AlarmSummary1"] = (ushort)0x0004,
            ["emu2.PcsList[1].AlarmSummary2"] = (ushort)0
        };
        var plugin = new TrinaEmuFaultWordPlugin();

        // emu2 模块2 告警汇总1 非零 → 故障总=1
        Assert.Equal(1, plugin.Compute("SystemFaultSummary", "emu1", sim));

        // 全部模块告警清零 → 故障总=0
        sim["emu2.PcsList[1].AlarmSummary1"] = (ushort)0;
        Assert.Equal(0, plugin.Compute("SystemFaultSummary", "emu1", sim));
    }

    [Fact]
    public void Compute_SystemFaultSummary_ProbesUnitsUntilMissing()
    {
        // 仅 emu1 存在：emu2 缺失时停止探测，emu1 两模块无告警 → 0
        var sim = new FakeSimulation
        {
            ["emu1.PcsList.Count"] = 2,
            ["emu1.PcsList[0].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[0].AlarmSummary2"] = (ushort)0,
            ["emu1.PcsList[1].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[1].AlarmSummary2"] = (ushort)0
        };
        var plugin = new TrinaEmuFaultWordPlugin();
        Assert.Equal(0, plugin.Compute("SystemFaultSummary", "emu1", sim));

        // 机组根路径无编号后缀：不探测，输出 0
        Assert.Equal(0, plugin.Compute("SystemFaultSummary", "emu", sim));
        Assert.True(plugin.CanHandle("SystemFaultSummary"));
    }

    [Fact]
    public void Compute_SystemRunStateSummary_FaultAlarmRunningStoppedPriority()
    {
        var plugin = new TrinaEmuFaultWordPlugin();

        // 全停机（OperationStatus=1，无告警）→ 1
        var sim = new FakeSimulation
        {
            ["emu1.PcsList.Count"] = 2,
            ["emu1.PcsList[0].OperationStatus"] = 1,
            ["emu1.PcsList[0].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[0].AlarmSummary2"] = (ushort)0,
            ["emu1.PcsList[1].OperationStatus"] = 1,
            ["emu1.PcsList[1].AlarmSummary1"] = (ushort)0,
            ["emu1.PcsList[1].AlarmSummary2"] = (ushort)0
        };
        Assert.Equal(1, plugin.Compute("SystemRunStateSummary", "emu1", sim));

        // 模块1 待机（仿真 2 → 协议 3 运行中）
        sim["emu1.PcsList[0].OperationStatus"] = 2;
        Assert.Equal(3, plugin.Compute("SystemRunStateSummary", "emu1", sim));

        // 模块2 放电运行仍为 3；叠加告警 → 6
        sim["emu1.PcsList[1].OperationStatus"] = 5;
        Assert.Equal(3, plugin.Compute("SystemRunStateSummary", "emu1", sim));
        sim["emu1.PcsList[1].AlarmSummary1"] = (ushort)0x0001;
        Assert.Equal(6, plugin.Compute("SystemRunStateSummary", "emu1", sim));

        // 模块1 故障（仿真 6 → 协议 5）优先级最高，覆盖告警
        sim["emu1.PcsList[0].OperationStatus"] = 6;
        Assert.Equal(5, plugin.Compute("SystemRunStateSummary", "emu1", sim));
    }

    [Fact]
    public void Compute_SystemRunStateSummary_AggregatesAcrossUnits()
    {
        var plugin = new TrinaEmuFaultWordPlugin();

        // emu1 停机，emu2 充电运行（仿真 4 → 3）
        var sim = new FakeSimulation
        {
            ["emu1.PcsList.Count"] = 2,
            ["emu1.PcsList[0].OperationStatus"] = 1,
            ["emu1.PcsList[1].OperationStatus"] = 1,
            ["emu2.PcsList.Count"] = 2,
            ["emu2.PcsList[0].OperationStatus"] = 4,
            ["emu2.PcsList[1].OperationStatus"] = 1
        };
        Assert.Equal(3, plugin.Compute("SystemRunStateSummary", "emu1", sim));

        // 无任何机组可探测 → 停机 1
        Assert.Equal(1, plugin.Compute("SystemRunStateSummary", "emu9", new FakeSimulation()));
        Assert.True(plugin.CanHandle("SystemRunStateSummary"));
    }

    [Fact]
    public void Registry_ResolvesFirstMatchingPlugin()
    {
        var registry = new TelemetryPluginRegistry().Register(new TrinaEmuFaultWordPlugin());

        Assert.NotNull(registry.Resolve("ModuleWarningWord1"));
        Assert.NotNull(registry.Resolve("ModuleWarningWord2"));
        Assert.Null(registry.Resolve("UnknownWord"));
        Assert.Null(registry.Resolve(""));
    }

    [Fact]
    public void FromPointMap_Trina10MW_HasPluginPointsForModuleWarningWords()
    {
        var catalog = LoadCatalog("trina_10MW");

        // 4 机组 × (模块1/2 × 警告字1/2) = 16 个插件点 + 系统故障总 + 系统总状态精简版
        Assert.Equal(18, catalog.PluginPoints.Count);
        Assert.All(catalog.PluginPoints, p =>
            Assert.Contains(p.WordKey, new[] { "ModuleWarningWord1", "ModuleWarningWord2", "SystemFaultSummary", "SystemRunStateSummary" }));

        // 设备根路径与机组/模块层级一致：emu{n}.PcsList[0/1]，系统级汇总为 emu1
        var roots = catalog.PluginPoints.Select(p => p.DeviceRoot).Distinct().OrderBy(r => r).ToArray();
        Assert.Equal(new[]
        {
            "emu1", "emu1.PcsList[0]", "emu1.PcsList[1]",
            "emu2.PcsList[0]", "emu2.PcsList[1]",
            "emu3.PcsList[0]", "emu3.PcsList[1]",
            "emu4.PcsList[0]", "emu4.PcsList[1]"
        }, roots);

        // 插件点不进普通反射遥测绑定
        var pluginParams = catalog.PluginPoints.Select(p => p.ParamName).ToHashSet();
        Assert.DoesNotContain(catalog.TelemetryPoints, t => pluginParams.Contains(t.ParamName));
    }

    [Fact]
    public void FromPointMap_Trina55MW_HasEightPluginPoints()
    {
        var catalog = LoadCatalog("trina_5.5MW");

        Assert.Equal(10, catalog.PluginPoints.Count);
        Assert.Contains(catalog.PluginPoints, p => p.DeviceRoot == "emu2.PcsList[1]" && p.WordKey == "ModuleWarningWord2");
    }

    [Fact]
    public void TelemetryPipeline_ComputesPluginPointsAndDeduplicates()
    {
        var entry = new MapEntry { FunctionCode = 4, Address = 2831, ParamName = "yc210", Scale = 1, Size = 16, Type = "u16" };
        var catalog = new PointCatalog
        {
            ServerName = "simEmu1",
            TelemetryPoints = Array.Empty<PointBinding>(),
            ControlPoints = Array.Empty<PointBinding>(),
            PluginPoints = new[]
            {
                new PluginPointBinding { Entry = entry, ParamName = "yc210", WordKey = "ModuleWarningWord1", DeviceRoot = Root }
            },
            DefaultValues = new Dictionary<string, object>()
        };

        var sim = new FakeSimulation { [$"{Root}.InsulationAlarm"] = true };
        var modbus = new RecordingModbusAdapter();
        var registry = new TelemetryPluginRegistry().Register(new TrinaEmuFaultWordPlugin());
        var pipeline = new TelemetryPipeline(catalog, sim, modbus, new ShadowStore(), registry);

        pipeline.RunOnce();
        Assert.Equal(3, modbus.LastWritten["yc210"]); // Bit0|Bit1

        // 值未变化：不重复写
        modbus.LastWritten.Clear();
        pipeline.RunOnce();
        Assert.Empty(modbus.LastWritten);

        // 故障变化后重新写出
        sim[$"{Root}.AcSurgeProtectorAbnormal"] = true;
        pipeline.RunOnce();
        Assert.Equal(3 | (1 << 12), modbus.LastWritten["yc210"]);
    }

    [Fact]
    public void TelemetryPipeline_UnhandledWordKey_KeepsDefault()
    {
        var entry = new MapEntry { FunctionCode = 4, Address = 2800, ParamName = "yc200", Scale = 1, Size = 16, Type = "u16" };
        var catalog = new PointCatalog
        {
            ServerName = "simEmu1",
            TelemetryPoints = Array.Empty<PointBinding>(),
            ControlPoints = Array.Empty<PointBinding>(),
            PluginPoints = new[]
            {
                new PluginPointBinding { Entry = entry, ParamName = "yc200", WordKey = "UnknownWord", DeviceRoot = Root }
            },
            DefaultValues = new Dictionary<string, object>()
        };

        var modbus = new RecordingModbusAdapter();
        var pipeline = new TelemetryPipeline(catalog, new FakeSimulation(), modbus, new ShadowStore(),
            new TelemetryPluginRegistry().Register(new TrinaEmuFaultWordPlugin()));

        pipeline.RunOnce();
        Assert.Empty(modbus.LastWritten); // 无插件覆盖，保持默认值 0
    }

    private static PointCatalog LoadCatalog(string modelDir)
    {
        var path = Path.Combine(FindRepoRoot(), "pointmaps", "models", "emu", modelDir, "emu.csv");
        Assert.True(File.Exists(path), path);
        var pointMap = new ModbusPointMap(path, "simEmu1");
        return PointCatalogLoader.FromPointMap(pointMap, "simEmu1", new DataExchangeOptions());
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

    /// <summary>字典式仿真数据假对象。</summary>
    private sealed class FakeSimulation : ISimulationDataAdapter
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public object? this[string path]
        {
            get => _values.TryGetValue(path, out var v) ? v : null;
            set
            {
                if (value == null) _values.Remove(path);
                else _values[path] = value;
            }
        }

        public object? Read(string fullBindingPath) => this[fullBindingPath];
        public bool Write(string fullBindingPath, object value) { this[fullBindingPath] = value; return true; }
    }

    /// <summary>记录写点调用的 Modbus 适配器假对象。</summary>
    private sealed class RecordingModbusAdapter : IModbusRegisterAdapter
    {
        public Dictionary<string, object> LastWritten { get; } = new();

        public void WriteDefaults(IReadOnlyDictionary<string, object> defaults)
        {
        }

        public void WritePoints(IReadOnlyDictionary<string, object> values, byte slaveId = 1, bool applyScale = true)
        {
            foreach (var kv in values)
                LastWritten[kv.Key] = kv.Value;
        }

        public Dictionary<string, object> ReadAllControlRaw(IReadOnlyList<string> paramNames, byte slaveId = 1) => new();
        public object? ReadParsedPoint(string paramName, byte slaveId = 1) => null;
    }
}

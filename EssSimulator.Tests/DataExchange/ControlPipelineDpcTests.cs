using EssSimulator.DataExchange.Adapters;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Effects;
using EssSimulator.DataExchange.Pipeline;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.Tests.DataExchange;

public class ControlPipelineDpcTests
{
    private sealed class FakeSimulationAdapter : ISimulationDataAdapter
    {
        private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

        public object? Read(string fullPath) =>
            _values.TryGetValue(fullPath, out var v) ? v : null;

        public bool Write(string fullPath, object value)
        {
            _values[fullPath] = value;
            return true;
        }
    }

    private sealed class FakeModbusAdapter : IModbusRegisterAdapter
    {
        private readonly Dictionary<string, object> _registers = new(StringComparer.OrdinalIgnoreCase);

        public void WriteDefaults(IReadOnlyDictionary<string, object> defaults) => WritePoints(defaults);

        public void WritePoints(IReadOnlyDictionary<string, object> values)
        {
            foreach (var pair in values)
                _registers[pair.Key] = pair.Value;
        }

        public void WritePoints(IReadOnlyDictionary<string, object> values, byte slaveId = 1, bool applyScale = true) =>
            WritePoints(values);

        public Dictionary<string, object> ReadAllControlRaw(IReadOnlyList<string> paramNames)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in paramNames)
            {
                if (_registers.TryGetValue(name, out var raw))
                    result[name] = raw;
            }

            return result;
        }

        public object? ReadParsedPoint(string paramName, byte slaveId = 1) =>
            _registers.TryGetValue(paramName, out var val) ? val : null;
    }

    private static PointBinding Yx3Binding() => new()
    {
        Entry = new MapEntry
        {
            Address = 1003,
            FunctionCode = 5,
            ParamName = "yx3",
            Size = 1,
            Type = "bool"
        },
        ParamName = "yx3",
        Target = new DataTarget { RootKey = "emu1", PropertyPath = "PcsList[0].pcsOnOffSwitch" },
        Semantics = ControlSemantics.Hold,
        Effect = ControlEffectId.PcsApplyCommands
    };

    private static ModbusParser CreateParser()
    {
        var pointMap = new ModbusPointMap("emu.csv", "simEmu1");
        return new ModbusParser(pointMap.RawMaps);
    }

    [Fact]
    public void RunOnce_DoesNotRevert_WhenModbusMatchesDpcStop()
    {
        var catalog = new PointCatalog
        {
            ServerName = "simEmu1",
            TelemetryPoints = Array.Empty<PointBinding>(),
            ControlPoints = new[] { Yx3Binding() },
            DefaultValues = new Dictionary<string, object>()
        };

        var simulation = new FakeSimulationAdapter();
        var modbus = new FakeModbusAdapter();
        var shadow = new ShadowStore();

        simulation.Write("emu1.PcsList[0].pcsOnOffSwitch", false);
        shadow.CommitControl("yx3", 0);
        modbus.WritePoints(new Dictionary<string, object> { { "yx3", 0 } });

        var control = new ControlPipeline(
            catalog, simulation, modbus, CreateParser(),
            shadow, new ControlEffectRegistry(), "simEmu1", logControlChanges: false);

        control.RunOnce();

        Assert.False(Convert.ToBoolean(simulation.Read("emu1.PcsList[0].pcsOnOffSwitch")!));
        Assert.Equal(0, Convert.ToInt32(modbus.ReadParsedPoint("yx3")!));
    }
}

using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Propagation;
using Xunit;

namespace EssSimulator.Tests.Propagation;

public class PcsBusVoltageSourceTests
{
    private sealed class FixedVoltageSource : IBusVoltageSource
    {
        public string SourceId => "fixed";
        public bool IsInjecting(DeviceStepContext context) => true;
        public (double LineVoltageV, double FrequencyHz) GetInjection(DeviceStepContext context) => (400, 48);
    }

    [Fact]
    public void ApplyLocalVoltageSources_overlays_pcs_injection_on_bus690()
    {
        var bus = new ElectricalBusNode("BUS_690_U0", 690) { LineVoltageV = 0 };
        bus.RegisterVoltageSource(new FixedVoltageSource());

        var sweep = new PropagationSweepContext
        {
            DeviceContext = new DeviceStepContext(),
            Step = TimeSpan.FromMilliseconds(100),
            Bus35 = new ElectricalBusNode("BUS_35", 35000),
            PcsCfg = new Configuration.PcsPhysicalConfig(),
            SystemFrequencyHz = 50,
            LastBus35LineVoltageV = 0,
            StationBusNominalLineVoltageV = 35000,
            MainBreakerClosed = false
        };

        Assert.True(bus.ApplyLocalVoltageSources(sweep));
        Assert.Equal(400, bus.LineVoltageV, 0);
        Assert.Equal(48, bus.FrequencyHz, 0);
    }
}

using EssSimulator.EssDeviceSimModel.Interface;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Propagation;
using Xunit;

namespace EssSimulator.Tests.Propagation;

public class ElectricalSignalRouterTests
{
    private sealed class StubDevice : IElectricalDevice
    {
        public StubDevice(string id) => DeviceId = id;
        public string DeviceId { get; }
        public ElectricalDeviceKind Kind => ElectricalDeviceKind.Grid;
        public IReadOnlyList<ElectricalPort> Ports => new[] { Port };
        public ElectricalPort Port { get; } = new() { PortId = "out" };
        public int CallbackCount { get; private set; }

        public void Step(DeviceStepContext context, TimeSpan step) { }

        public void OnOutput(ElectricalOutputChangedEventArgs args) => CallbackCount++;
    }

    [Fact]
    public void Publish_notifies_registered_subscriber()
    {
        var router = new ElectricalSignalRouter();
        var source = new StubDevice("grid");
        var target = new StubDevice("xfmr");
        source.Port.Output = ElectricalPortSnapshot.FromAc(new AcInternalQuantities { LineVoltageV = 220000 });

        router.Subscribe(source, source.Port, _ => target.OnOutput(_));
        Assert.Equal(1, router.SubscriberCount(new ElectricalPortRef("grid", "out")));

        var ctx = new DeviceStepContext();
        router.Publish(source, source.Port, ctx, TimeSpan.FromMilliseconds(100));

        Assert.Equal(1, target.CallbackCount);
    }
}

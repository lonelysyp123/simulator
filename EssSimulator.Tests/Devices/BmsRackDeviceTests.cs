using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Battery;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;

namespace EssSimulator.Tests.Devices;

public class BmsRackDeviceTests
{
    [Fact]
    public void SetPcsLinked_UpdatesRackState()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig { ClusterCount = 1 });
        var device = new BmsRackDevice("bms_test", rack);

        device.SetPcsLinked(true);
        Assert.True(device.IsLinked);
        Assert.True(rack.GetRackState()!.IsPcsLinked);

        device.SetPcsLinked(false);
        Assert.False(device.IsLinked);
    }

    [Fact]
    public void SyncTelemetryAndProtection_SetsChargeFaultWhenSocHigh()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig { ClusterCount = 1 });
        var device = new BmsRackDevice("bms_test", rack);
        var bmsData = BmsDataGenerator.GenerateSampleData(1, 1);

        var rackState = rack.GetRackState()!;
        rackState.MinClusterSOC = 0.96;
        rackState.MaxClusterSOC = 0.96;
        rackState.StateOfHealth = 1.0;

        device.SyncTelemetryAndProtection(bmsData);

        Assert.Equal((ushort)1, device.FaultCode);
        Assert.True(device.HasBlockingFault);
    }

    [Fact]
    public void UpdatePhysics_RefreshesDcPortVoltageWhenLinked()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig { ClusterCount = 1 });
        var device = new BmsRackDevice("bms_test", rack);
        device.SetPcsLinked(true);

        device.UpdatePhysics(0, 25.0, DateTime.Now, TimeSpan.FromMilliseconds(200));

        var voltage = device.Port.Output.Dc?.VoltageV ?? 0;
        Assert.True(voltage > 0);
    }

    [Fact]
    public void UpdatePhysics_ZeroVoltageWhenUnlinked()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig { ClusterCount = 1 });
        var device = new BmsRackDevice("bms_test", rack);
        device.SetPcsLinked(false);

        device.UpdatePhysics(10, 25.0, DateTime.Now, TimeSpan.FromMilliseconds(200));

        Assert.Equal(0, device.Port.Output.Dc?.VoltageV ?? -1);
    }
}

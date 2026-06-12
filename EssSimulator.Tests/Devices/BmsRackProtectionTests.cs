using EssSimulator.EssDeviceSimModel.Devices;

namespace EssSimulator.Tests.Devices;

public class BmsRackProtectionTests
{
    [Fact]
    public void UpdateUnder_EscalatesFromAlarmToFault()
    {
        bool? l1 = false, l2 = true, l3 = false;
        BmsRackProtection.UpdateUnder(ref l1, ref l2, ref l3,
            t1: 100, t2: 90, t3: 80,
            r1: 105, r2: 95, r3: 85,
            val: 75);

        Assert.False(l1);
        Assert.False(l2);
        Assert.True(l3);
    }

    [Fact]
    public void UpdateOver_RecoversFromFaultToAlarm()
    {
        bool? l1 = false, l2 = false, l3 = true;
        BmsRackProtection.UpdateOver(ref l1, ref l2, ref l3,
            t1: 50, t2: 55, t3: 60,
            r1: 48, r2: 53, r3: 58,
            val: 57);

        Assert.False(l3);
        Assert.True(l2);
    }

    [Fact]
    public void UpdateUnder_TriggersProtectionWhenBelowThreshold()
    {
        bool? l1 = null, l2 = null, l3 = null;
        BmsRackProtection.UpdateUnder(ref l1, ref l2, ref l3,
            t1: 600, t2: 580, t3: 560,
            r1: 610, r2: 590, r3: 570,
            val: 595);

        Assert.True(l1);
        Assert.Null(l2);
        Assert.Null(l3);
    }
}

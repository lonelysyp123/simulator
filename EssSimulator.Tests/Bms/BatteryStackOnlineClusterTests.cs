using EssSimulator.EssSimModelApi.BatteryManagementSystem;

namespace EssSimulator.Tests.Bms;

public class BatteryStackOnlineClusterTests
{
    [Fact]
    public void OnlineClusterCount_IsZeroWhenOffGrid()
    {
        var stack = new BatteryStack { IsPcsLinked = false, ManagedClusterCount = 12 };
        for (int i = 0; i < 12; i++)
            stack.Cluseter.Add(new BatteryCluster { ClusterId = i + 1 });

        Assert.Equal(12, stack.TotalClusterCount);
        Assert.Equal(0, stack.OnlineClusterCount);
    }

    [Fact]
    public void OnlineClusterCount_EqualsTotalWhenGridLinked()
    {
        var stack = new BatteryStack { IsPcsLinked = true, ManagedClusterCount = 12 };
        for (int i = 0; i < 12; i++)
            stack.Cluseter.Add(new BatteryCluster { ClusterId = i + 1 });

        Assert.Equal(12, stack.OnlineClusterCount);
    }
}

using EssSimulator.EssDeviceSimModel;
using Xunit;

namespace EssSimulator.Tests.Battery;

public class RackSetSocTests
{
    private static BatteryRackSimulator CreateSmallRack(double initialSoc = 0.5) =>
        new(new RackConfiguration
        {
            ClusterCount = 2,
            ClusterConfig = new ClusterConfiguration
            {
                PackCount = 1,
                ClusterInternalResistance = 0.1,
                PackConfig = new PackConfiguration
                {
                    SeriesCount = 2,
                    ParallelCount = 1,
                    NominalVoltage = 3.2,
                    NominalCapacity = 100,
                    InitialSoc = initialSoc,
                    InitialSocRandomRange = 0,
                    PackInternalResistance = 0.05
                }
            }
        });

    [Fact]
    public void SetSoc_UpdatesAllCellsAndRackMinClusterSoc()
    {
        var rack = CreateSmallRack(0.5);

        Assert.True(rack.TrySetSoc(0.72, out var message));
        Assert.Contains("0.72", message);

        Assert.Equal(0.72, rack.GetRackSOC(), 3);
        Assert.Equal(0.72, rack.GetRackState()!.MinClusterSOC, 3);
        Assert.Equal(0.72, rack.GetRackState()!.MaxClusterSOC, 3);

        foreach (var cluster in rack._clusters)
        {
            Assert.Equal(0.72, cluster.GetClusterSOC(), 3);
            foreach (var pack in cluster._packs)
                Assert.Equal(0.72, pack.GetPackSOC(), 3);
        }
    }

    [Fact]
    public void SetSoc_RejectsOutOfRange()
    {
        var rack = CreateSmallRack();

        Assert.False(rack.TrySetSoc(-0.01, out _));
        Assert.False(rack.TrySetSoc(1.01, out _));
        Assert.Equal(0.5, rack.GetRackSOC(), 2);
    }

    [Fact]
    public void SetSoc_PreservesPcsLinkFlag()
    {
        var rack = CreateSmallRack();
        rack.GetRackState()!.IsPcsLinked = true;

        Assert.True(rack.TrySetSoc(0.3, out _));
        Assert.True(rack.GetRackState()!.IsPcsLinked);
        Assert.Equal(0.3, rack.GetRackSOC(), 3);
    }
}

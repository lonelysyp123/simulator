using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Battery;
using Xunit;

namespace EssSimulator.Tests.Battery;

public class InitialSocExactTests
{
    [Fact]
    public void CreateRack_UsesConfiguredSocExactly_IgnoringLegacyRandomRange()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig
        {
            ClusterCount = 2,
            PackCount = 2,
            CellSeriesCount = 4,
            CellParallelCount = 1,
            CellInitialSoc = 0.58,
            CellInitialSocRandomRange = 0.05 // 遗留配置：建模时不再施加扰动
        });

        Assert.Equal(0.58, rack.GetRackSOC(), 5);
        Assert.Equal(0.58, rack.GetRackState()!.MinClusterSOC, 5);
        Assert.Equal(0.58, rack.GetRackState()!.MaxClusterSOC, 5);

        foreach (var cluster in rack._clusters)
        {
            Assert.Equal(0.58, cluster.GetClusterSOC(), 5);
            foreach (var pack in cluster._packs)
                Assert.Equal(0.58, pack.GetPackSOC(), 5);
        }
    }
}

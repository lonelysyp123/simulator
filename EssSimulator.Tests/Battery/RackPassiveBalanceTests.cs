using EssSimulator.EssDeviceSimModel;
using Xunit;

namespace EssSimulator.Tests.Battery;

public class RackPassiveBalanceTests
{
    private static BatteryRackSimulator CreateTwoClusterRack(RackPassiveBalanceConfig balance) =>
        new(new RackConfiguration
        {
            ClusterCount = 2,
            PassiveBalance = balance,
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
                    InitialSoc = 0.55,
                    InitialSocRandomRange = 0,
                    PackInternalResistance = 0.05
                }
            }
        });

    [Fact]
    public void ApplyPassiveBalance_BleedsHighSocCluster_WhenDeltaAboveStart()
    {
        var rack = CreateTwoClusterRack(new RackPassiveBalanceConfig
        {
            Enabled = true,
            StartSocDelta = 0.02,
            StopSocDelta = 0.01,
            BalanceCRate = 0.05,
            IdleOnly = true,
            IdleCurrentThresholdA = 10,
            BleedAboveMinMargin = 0.01
        });

        // 人为拉开簇 SOC：簇0 多放一些电
        var t0 = DateTime.UtcNow;
        rack._clusters[0].Update(-50, 25, t0, TimeSpan.FromHours(0.2)); // ΔSOC≈-0.1
        double soc0 = rack._clusters[0].GetClusterSOC();
        double soc1 = rack._clusters[1].GetClusterSOC();
        Assert.True(soc1 - soc0 > 0.05, $"expected imbalance, got {soc1:F3}-{soc0:F3}");

        var currents = new[] { 0.0, 0.0 };
        double sum = rack.ApplyPassiveBalanceCurrents(currents, rackCurrent: 0);

        Assert.True(sum > 0);
        // 高 SOC 簇（簇1）应叠加放电（电流更负）
        Assert.True(currents[1] < currents[0]);
        Assert.True(currents[1] < 0);
    }

    [Fact]
    public void PassiveBalance_ReducesSocDelta_OverIdleSteps()
    {
        var rack = CreateTwoClusterRack(new RackPassiveBalanceConfig
        {
            Enabled = true,
            StartSocDelta = 0.02,
            StopSocDelta = 0.005,
            BalanceCRate = 0.2, // 测试加速
            IdleOnly = true,
            IdleCurrentThresholdA = 10,
            BleedAboveMinMargin = 0.005
        });

        var t0 = DateTime.UtcNow;
        rack._clusters[0].Update(-40, 25, t0, TimeSpan.FromHours(0.25));
        double delta0 = Math.Abs(rack._clusters[1].GetClusterSOC() - rack._clusters[0].GetClusterSOC());
        Assert.True(delta0 > 0.05);

        for (int i = 0; i < 80; i++)
        {
            rack.Update(0, 25, t0.AddSeconds(i), TimeSpan.FromSeconds(60));
        }

        double delta1 = rack.GetRackState().SOCDifference;
        Assert.True(delta1 < delta0, $"delta should shrink: {delta0:F4} -> {delta1:F4}");
        Assert.True(rack.GetRackState().IsPassiveBalancing || delta1 <= 0.02);
    }

    [Fact]
    public void PassiveBalance_Skipped_WhenChargingHard()
    {
        var rack = CreateTwoClusterRack(new RackPassiveBalanceConfig
        {
            Enabled = true,
            StartSocDelta = 0.02,
            BalanceCRate = 0.05,
            IdleOnly = true,
            IdleCurrentThresholdA = 10
        });

        var t0 = DateTime.UtcNow;
        rack._clusters[0].Update(-50, 25, t0, TimeSpan.FromHours(0.2));

        var currents = new[] { 20.0, 20.0 };
        double sum = rack.ApplyPassiveBalanceCurrents(currents, rackCurrent: 200);
        Assert.Equal(0, sum);
        Assert.Equal(20.0, currents[0]);
        Assert.Equal(20.0, currents[1]);
    }
}

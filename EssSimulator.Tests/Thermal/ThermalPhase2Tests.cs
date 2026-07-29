using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Battery;
using EssSimulator.EssDeviceSimModel.Thermal;

namespace EssSimulator.Tests.Thermal;

public class ClimateModelTests
{
    [Fact]
    public void FixedCelsius_IgnoresTimeOfDay()
    {
        var climate = new ClimateModel(new ClimateConfig { FixedCelsius = 22 });
        Assert.Equal(22, climate.EvaluateOutdoorCelsius(new DateTime(2026, 7, 29, 2, 0, 0)));
        Assert.Equal(22, climate.EvaluateOutdoorCelsius(new DateTime(2026, 7, 29, 14, 0, 0)));
    }

    [Fact]
    public void Diurnal_PeakHourWarmerThanOpposite()
    {
        var climate = new ClimateModel(new ClimateConfig
        {
            MinCelsius = 10,
            MaxCelsius = 30,
            PeakHour = 14
        });

        double peak = climate.EvaluateOutdoorCelsius(new DateTime(2026, 7, 29, 14, 0, 0));
        double trough = climate.EvaluateOutdoorCelsius(new DateTime(2026, 7, 29, 2, 0, 0));

        Assert.InRange(peak, 29.5, 30.5);
        Assert.InRange(trough, 9.5, 10.5);
        Assert.True(peak > trough);
    }
}

public class ThermalNetworkTests
{
    [Fact]
    public void HeatFlowsFromHotToCold()
    {
        var net = new ThermalNetwork();
        net.AddNode(new ThermalNode("hot", 1000, 40));
        net.AddNode(new ThermalNode("cold", 1000, 20));
        net.AddEdge(new ThermalEdge("hot", "cold", 0.1));

        net.Step(TimeSpan.FromSeconds(10));

        Assert.True(net.GetNode("hot").TemperatureCelsius < 40);
        Assert.True(net.GetNode("cold").TemperatureCelsius > 20);
    }

    [Fact]
    public void BoundaryNode_TemperaturePinned()
    {
        var net = new ThermalNetwork();
        net.AddNode(new ThermalNode("out", 1, 15, isBoundary: true));
        net.AddNode(new ThermalNode("air", 5000, 25));
        net.AddEdge(new ThermalEdge("out", "air", 0.05));

        for (int i = 0; i < 200; i++)
            net.Step(TimeSpan.FromSeconds(1));

        Assert.Equal(15, net.GetNode("out").TemperatureCelsius);
        // τ≈C/G=250s，200s 约走完一半温差（25→15）
        Assert.InRange(net.GetNode("air").TemperatureCelsius, 18.5, 20.5);
    }
}

public class BmsCabinetThermalZoneTests
{
    [Fact]
    public void WithoutHvac_BatteryHeatRaisesCabinetAir()
    {
        var cfg = new BmsCabinetThermalConfig
        {
            HvacCoolingPowerW = 0,
            AirThermalCapacityJPerK = 20_000,
            ShellThermalCapacityJPerK = 50_000,
            BatteryThermalCapacityJPerK = 50_000,
            OutdoorToShellResistanceKPerW = 0.2,
            ShellToAirResistanceKPerW = 0.05,
            BatteryToAirResistanceKPerW = 0.02
        };

        var zone = new BmsCabinetThermalZone("bms1", cfg, initialTempCelsius: 25);
        double t0 = zone.CabinetAirCelsius;

        for (int i = 0; i < 300; i++)
        {
            zone.PendingBatteryHeatW = 5000; // 5 kW 持续注入
            zone.Step(TimeSpan.FromSeconds(1), outdoorCelsius: 25);
        }

        Assert.True(zone.CabinetAirCelsius > t0 + 2);
        Assert.True(zone.BatteryNodeCelsius > zone.CabinetAirCelsius);
    }
}

public class PlantThermalSystemTests
{
    [Fact]
    public void Disabled_ReturnsFixedOrDefaultAmbient()
    {
        var thermal = new PlantThermalSystem(
            new ThermalRuntimeConfig
            {
                Enabled = false,
                Climate = new ClimateConfig { FixedCelsius = 18 }
            },
            bmsChannelCount: 2,
            initialTime: DateTime.UtcNow);

        Assert.Equal(18, thermal.GetBmsAmbientCelsius(0));
        Assert.Equal(18, thermal.GetBmsAmbientCelsius(1));
    }

    [Fact]
    public void EstimateRackOhmicLoss_ScalesWithCurrentSquared()
    {
        var rack = BmsRackFactory.CreateRack(new BmsDeviceConfig
        {
            ClusterCount = 2,
            PackCount = 2,
            CellSeriesCount = 10,
            CellParallelCount = 1
        });

        double p1 = PlantThermalSystem.EstimateRackOhmicLossWatts(rack, 100);
        double p2 = PlantThermalSystem.EstimateRackOhmicLossWatts(rack, 200);
        Assert.True(p1 > 0);
        Assert.InRange(p2 / p1, 3.9, 4.1);
    }
}

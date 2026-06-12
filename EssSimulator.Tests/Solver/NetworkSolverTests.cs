using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Solver;

namespace EssSimulator.Tests.Solver;

public class NetworkSolverTests
{
    private static ElectricalNetwork CreateDefaultNetwork()
    {
        var simCfg = new SimulatorConfig
        {
            Devices = { new EssUnitConfig() }
        };

        return NetworkTopologyBuilder.Build(
            simCfg,
            new PcsPhysicalConfig(),
            new TransformerConfig(),
            new UnitTransformerConfig(),
            new LoadConfig(),
            new PccConfig());
    }

    [Fact]
    public void MainBreakerClosed_PccVoltageNearNominal()
    {
        var network = CreateDefaultNetwork();
        network.MainBreaker.ApplyCommand(new DeviceCommand { Kind = DeviceCommandKind.CloseBreaker });
        foreach (var ub in network.UnitBreakers)
            ub.ApplyCommand(new DeviceCommand { Kind = DeviceCommandKind.CloseBreaker });

        var step = TimeSpan.FromMilliseconds(200);
        network.Solver.Step(step, step);

        Assert.InRange(network.PccLineVoltageV, 210_000, 230_000);
        Assert.InRange(network.StationBus35LineVoltageV, 33_000, 37_000);
    }

    [Fact]
    public void MainBreakerOpen_PccVoltageZero()
    {
        var network = CreateDefaultNetwork();
        network.MainBreaker.ApplyCommand(new DeviceCommand { Kind = DeviceCommandKind.OpenBreaker });

        var step = TimeSpan.FromMilliseconds(200);
        network.Solver.Step(step, step);

        Assert.Equal(0, network.PccLineVoltageV);
    }

    [Fact]
    public void MainBreakerOpen_PccMeterReportsZeroPower()
    {
        var network = CreateDefaultNetwork();
        network.MainBreaker.ApplyCommand(new DeviceCommand { Kind = DeviceCommandKind.OpenBreaker });

        var step = TimeSpan.FromMilliseconds(200);
        network.Solver.Step(step, step);

        Assert.Equal(0, network.PccMeter.Telemetry.Primary.ActivePowerKw);
        Assert.Equal(0, network.PccMeter.Telemetry.Primary.ReactivePowerKvar);
    }
}

using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Devices;

public class BreakerSimulatorTests
{
    [Fact]
    public void OpenBreaker_BlocksCurrent_ButKeepsSecondaryVoltage()
    {
        var breaker = new BreakerSimulator("main_breaker", new BreakerBranchConfig { InitialClosed = false });

        breaker.Primary.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
        {
            Connection = ThreePhaseConnection.Star,
            LineVoltageV = 220_000,
            FrequencyHz = 50
        });
        breaker.Secondary.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
        {
            Connection = ThreePhaseConnection.Star,
            LineVoltageV = 35_000,
            LineCurrentA = 500,
            FrequencyHz = 50
        });

        breaker.Step(new DeviceStepContext(), TimeSpan.FromMilliseconds(200));

        Assert.Equal(0, breaker.Primary.Output.Ac!.Internal.LineCurrentA);
        Assert.Equal(0, breaker.Secondary.Output.Ac!.Internal.LineCurrentA);
        Assert.Equal(35_000, breaker.Secondary.Output.Ac.Internal.LineVoltageV);
    }

    [Fact]
    public void ClosedBreaker_PassesVoltageAndCurrent()
    {
        var breaker = new BreakerSimulator("main_breaker", new BreakerBranchConfig { InitialClosed = true });

        breaker.Primary.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
        {
            Connection = ThreePhaseConnection.Star,
            LineVoltageV = 220_000,
            FrequencyHz = 50
        });
        breaker.Secondary.Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
        {
            Connection = ThreePhaseConnection.Star,
            LineCurrentA = -141,
            ActivePowerKw = 50_000,
            FrequencyHz = 50
        });

        breaker.Step(new DeviceStepContext(), TimeSpan.FromMilliseconds(200));

        Assert.Equal(220_000, breaker.Secondary.Output.Ac!.Internal.LineVoltageV);
        Assert.Equal(-141, breaker.Secondary.Output.Ac.Internal.LineCurrentA);
    }
}

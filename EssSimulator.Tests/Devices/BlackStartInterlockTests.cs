using EssSimulator.EssDeviceSimModel;

namespace EssSimulator.Tests.Devices;

public class BlackStartInterlockTests
{
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(false, false, false, false)]
    public void IsStationShortCircuitRisk_MatchesBreakerCombination(
        bool mainClosed, bool unitClosed, bool blackStart, bool expected)
    {
        bool actual = BlackStartInterlock.IsStationShortCircuitRisk(mainClosed, unitClosed, blackStart);
        Assert.Equal(expected, actual);
    }
}

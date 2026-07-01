using EssSimulator.EssDeviceSimModel;
using Xunit;

namespace EssSimulator.Tests.Devices;

public class PcsDisplayLabelsTests
{
    [Theory]
    [InlineData(OperationMode.Off, false, 0, false, 0, false, 1)]
    [InlineData(OperationMode.Normal, false, 0, false, 0, false, 1)]
    [InlineData(OperationMode.Standby, true, 0, false, 0, false, 2)]
    [InlineData(OperationMode.Normal, true, 100, false, 0, false, 5)]
    [InlineData(OperationMode.Normal, true, -100, false, 0, false, 4)]
    [InlineData(OperationMode.Normal, true, 0, false, 0, false, 2)]
    [InlineData(OperationMode.Normal, true, 0, true, 0, false, 2)]
    [InlineData(OperationMode.Normal, true, 0, false, 1, false, 6)]
    [InlineData(OperationMode.Normal, true, 0, false, 0, true, 6)]
    public void ToOperationStatusCode_UsesProjectOperationStatusCodes(
        OperationMode mode, bool run, double powerKw, bool blackStart, ushort fault, bool alarm, int expected)
    {
        int code = PcsDisplayLabels.ToOperationStatusCode(mode, run, powerKw, blackStart, fault, alarm);
        Assert.Equal(expected, code);
    }
}

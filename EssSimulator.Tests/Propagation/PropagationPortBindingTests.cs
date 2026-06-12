using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Propagation;

namespace EssSimulator.Tests.Propagation;

public class PropagationPortBindingTests
{
    [Fact]
    public void SetAcCurrentInput_applies_line_voltage_from_intent()
    {
        var port = new ElectricalPort
        {
            Input = ElectricalPortSnapshot.FromAc(new AcInternalQuantities
            {
                Connection = ThreePhaseConnection.Star,
                LineVoltageV = 0
            })
        };

        var intent = AcQuantityConverter.FromLineVoltageAndPower(
            35_000, -500, 0, ThreePhaseConnection.Star, 50);

        PropagationPortBinding.SetAcCurrentInput(port, intent);
        var read = AcPortHelper.ReadAcInput(port);

        Assert.Equal(35_000, read.LineVoltageV, 1.0);
        Assert.Equal(-500, read.ActivePowerKw, 5.0);
        Assert.InRange(read.LineCurrentA, 8.0, 8.5);
    }
}

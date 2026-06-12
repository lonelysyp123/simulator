using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Model;

public class AcQuantityConverterTests
{
    private const double Tolerance = 0.5;

    [Fact]
    public void Star_220kV_50MW_20Mvar_ProducesExpectedTerminalQuantities()
    {
        var internalQty = AcQuantityConverter.FromLineVoltageAndPower(
            lineVoltageV: 220_000,
            activePowerKw: 50_000,
            reactivePowerKvar: 20_000,
            connection: ThreePhaseConnection.Star,
            frequencyHz: 50);

        var terminal = AcQuantityConverter.ToTerminal(internalQty);

        Assert.Equal(220_000, terminal.Vab, Tolerance);
        Assert.Equal(127_017, terminal.Van, 1.0);
        Assert.Equal(141.2, Math.Abs(internalQty.LineCurrentA), 0.5);
        Assert.Equal(141.2, terminal.Ia, 0.5);
    }

    [Fact]
    public void Star_690V_2_5MW_ZeroQ_ProducesExpectedTerminalQuantities()
    {
        var internalQty = AcQuantityConverter.FromLineVoltageAndPower(
            lineVoltageV: 690,
            activePowerKw: 2_500,
            reactivePowerKvar: 0,
            connection: ThreePhaseConnection.Star,
            frequencyHz: 50);

        var terminal = AcQuantityConverter.ToTerminal(internalQty);

        Assert.Equal(690, terminal.Vab, Tolerance);
        Assert.Equal(398.4, terminal.Van, 0.2);
        Assert.Equal(2092, Math.Abs(internalQty.LineCurrentA), 1.0);
        Assert.Equal(2092, terminal.Ia, 1.0);
    }

    [Fact]
    public void Delta_690V_2_5MW_ZeroQ_PhaseCurrentIsLineOverSqrt3()
    {
        var internalQty = AcQuantityConverter.FromLineVoltageAndPower(
            lineVoltageV: 690,
            activePowerKw: 2_500,
            reactivePowerKvar: 0,
            connection: ThreePhaseConnection.Delta,
            frequencyHz: 50);

        var terminal = AcQuantityConverter.ToTerminal(internalQty);

        Assert.Equal(690, terminal.Vab, Tolerance);
        Assert.Equal(690, terminal.Van, Tolerance);
        Assert.Equal(2092, Math.Abs(internalQty.LineCurrentA), 1.0);
        Assert.Equal(1208, terminal.Ia, 1.0);
    }

    [Fact]
    public void FromTerminal_RoundTripsStarInternalQuantities()
    {
        var original = AcQuantityConverter.FromLineVoltageAndPower(
            690, 1000, 200, ThreePhaseConnection.Star);

        var terminal = AcQuantityConverter.ToTerminal(original);
        var roundTrip = AcQuantityConverter.FromTerminal(terminal);

        Assert.Equal(original.LineVoltageV, roundTrip.LineVoltageV, Tolerance);
        Assert.Equal(original.LineCurrentA, roundTrip.LineCurrentA, Tolerance);
    }

    [Fact]
    public void FromTerminal_RoundTripsDeltaInternalQuantities()
    {
        var original = AcQuantityConverter.FromLineVoltageAndPower(
            690, 1000, 200, ThreePhaseConnection.Delta);

        var terminal = AcQuantityConverter.ToTerminal(original);
        var roundTrip = AcQuantityConverter.FromTerminal(terminal);

        Assert.Equal(original.LineVoltageV, roundTrip.LineVoltageV, Tolerance);
        Assert.Equal(original.LineCurrentA, roundTrip.LineCurrentA, Tolerance);
    }

    [Fact]
    public void FromLineVoltageAndPower_RoundTripsActiveAndReactivePower()
    {
        var qty = AcQuantityConverter.FromLineVoltageAndPower(
            220_000, 50_000, 20_000, ThreePhaseConnection.Star);

        Assert.Equal(50_000, qty.ActivePowerKw, 1.0);
        Assert.Equal(20_000, qty.ReactivePowerKvar, 1.0);
        Assert.Equal(141.2, qty.LineCurrentA, 0.5);
        Assert.Equal(21.8, qty.PhaseAngleDeg, 0.5);
    }

    [Fact]
    public void FromLineVoltageAndPower_ImportPower_UsesQuadrantPhaseAngle()
    {
        var qty = AcQuantityConverter.FromLineVoltageAndPower(
            220_000, -50_000, 20_000, ThreePhaseConnection.Star);

        Assert.Equal(-50_000, qty.ActivePowerKw, 1.0);
        Assert.Equal(20_000, qty.ReactivePowerKvar, 1.0);
    }

    [Fact]
    public void MeterPtCt_ScalesPrimaryToSecondaryAndBack()
    {
        var primary = AcQuantityConverter.FromLineVoltageAndPower(
            220_000, 50_000, 20_000, ThreePhaseConnection.Star);

        var pt = new PtConfig { PrimaryLineVoltageV = 220_000, SecondaryLineVoltageV = 100 };
        var ct = new CtConfig { PrimaryCurrentA = 2000, SecondaryCurrentA = 5 };

        var secondary = MeterQuantityConverter.ToSecondary(primary, pt, ct);
        var reported = MeterQuantityConverter.ToReportedPrimary(
            secondary, pt, ct, MeterReportedQuantity.Primary);

        Assert.Equal(100, secondary.LineVoltageV, 0.01);
        Assert.Equal(141.2 / 400, Math.Abs(secondary.LineCurrentA), 0.001);
        Assert.Equal(220_000, reported.LineVoltageV, 1.0);
        Assert.Equal(141.2, Math.Abs(reported.LineCurrentA), 0.5);
    }
}

using EssSimulator.EssDeviceSimModel.Propagation;
using Xunit;

namespace EssSimulator.Tests.Propagation;

public class QuvConvergenceTests
{
    [Theory]
    [InlineData(220000, 220300, 220000, 0.001, false)]
    [InlineData(220000, 220050, 220000, 0.001, true)]
    [InlineData(0, 0, 220000, 0.001, true)]
    public void IsLineVoltageConverged_respects_relative_tolerance(
        double previousV,
        double currentV,
        double nominalV,
        double tolerancePu,
        bool expected)
    {
        Assert.Equal(
            expected,
            QuvConvergence.IsLineVoltageConverged(previousV, currentV, nominalV, tolerancePu));
    }

    [Fact]
    public void GridQuShift_changes_with_feedback_reactive()
    {
        const double nominal = 220000;
        const double shortCircuitMva = 5000;
        const double k = 1.0;
        const double maxShift = 5.0;

        double vIntent = EssSimulator.EssDeviceSimModel.GridFeedbackConventions.CalculatePccLineVoltage(
            nominal, 100, shortCircuitMva, k, maxShift);
        double vFeedback = EssSimulator.EssDeviceSimModel.GridFeedbackConventions.CalculatePccLineVoltage(
            nominal, 500, shortCircuitMva, k, maxShift);

        Assert.NotEqual(vIntent, vFeedback);
    }
}

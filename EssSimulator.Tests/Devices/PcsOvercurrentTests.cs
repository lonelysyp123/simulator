using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Devices;

public class PcsOvercurrentTests
{
    private static PcsDevice CreateGridPcs(double maxCurrentA = 1100) =>
        PcsDeviceFactory.Create("pcs_test", new PcsDeviceConfig
        {
            AcNominalLineVoltageV = 690,
            FrequencyHz = 50,
            DcVoltageRangeMinV = 1000,
            DcVoltageRangeMaxV = 1500,
            MaxCurrentA = maxCurrentA,
            RatedPowerKw = 1250,
            MaxPowerKw = 1250,
            Efficiency = 0.99,
            GridLossCoefficient = 0.11,
            RampSlope = 500,
            RampIntervalMs = 100,
            RampDelayMs = 0,
        });

    [Fact]
    public void GridConnected_FullChargeAtRatedPower_DoesNotTripOvercurrent()
    {
        var pcs = CreateGridPcs(maxCurrentA: 1100);
        pcs.SyncExternalRunCommand(true);
        pcs.UpdateGridState(690, 50, isUtilityGridAvailable: true);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.GridConnected);
        pcs.SetPowerCommand(-1250, 0);

        for (int i = 0; i < 30; i++)
            pcs.Update(1300, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));

        var st = pcs.GetCurrentState();
        Assert.Equal(OperationMode.Normal, st.Mode);
        Assert.Equal(0, st.FaultType);
        Assert.InRange(Math.Abs(st.AcCurrent), 1000, 1100);
        Assert.True(Math.Abs(st.AcCurrent) <= 1100);
    }

    [Fact]
    public void GridConnected_AcCurrent_UsesTerminalPower_NotGridLossInflatedPower()
    {
        var pcs = CreateGridPcs(maxCurrentA: 2000);
        pcs.SyncExternalRunCommand(true);
        pcs.UpdateGridState(690, 50, isUtilityGridAvailable: true);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.GridConnected);
        pcs.SetPowerCommand(-1250, 0);

        for (int i = 0; i < 30; i++)
            pcs.Update(1300, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(100));

        var st = pcs.GetCurrentState();
        double expectedI = 1250000 / (690 * Math.Sqrt(3));
        Assert.InRange(Math.Abs(st.AcCurrent), expectedI - 5, expectedI + 5);
        Assert.True(Math.Abs(pcs.GetGridSideActivePower()) > Math.Abs(st.ActivePower));
    }
}

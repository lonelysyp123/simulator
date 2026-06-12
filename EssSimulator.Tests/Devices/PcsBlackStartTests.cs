using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Tests.Devices;

public class PcsBlackStartTests
{
    private static PcsDevice CreateDevice() =>
        PcsDeviceFactory.Create("pcs_test", new PcsDeviceConfig
        {
            AcNominalLineVoltageV = 690,
            FrequencyHz = 50,
            DcVoltageRangeMinV = 1000,
            DcVoltageRangeMaxV = 1500,
            MaxCurrentA = 1000,
            RatedPowerKw = 1725,
            MaxPowerKw = 1897.5,
            BlackStartPrechargeDelayMs = 0,
            BlackStartVoltageRampVs = 200,
            BlackStartFrequencyStartHz = 47,
            BlackStartCurrentLimitFraction = 0.3
        });

    [Fact]
    public void ApplyBlackStartEnabled_StartsInPreparingPhase()
    {
        var pcs = CreateDevice();
        pcs.ApplyBlackStartEnabled(true);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.Islanded);

        Assert.Equal(BlackStartPhase.Preparing, pcs.GetBlackStartPhase());
        Assert.False(pcs.IsBlackStartActive);
    }

    [Fact]
    public void AdvancePhase_TransitionsToSoftStartingAfterPrecharge()
    {
        var pcs = CreateDevice();
        pcs.ApplyBlackStartEnabled(true);
        pcs.UpdateGridState(0, 50, false);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.Islanded);
        pcs.Update(1200, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(200));

        Assert.Equal(BlackStartPhase.SoftStarting, pcs.GetBlackStartPhase());
        Assert.True(pcs.IsBlackStartActive);
    }

    [Fact]
    public void ClosedLoopBuild_UsesBusVoltageForPowerControl()
    {
        var pcs = CreateDevice();
        pcs.ApplyBlackStartEnabled(true);
        pcs.ApplyIslandVoltageCommand(690);
        pcs.UpdateGridState(0, 50, false);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.Islanded);
        pcs.Update(1200, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(400));
        pcs.RefreshBlackStartBusContext(25);

        pcs.Update(1200, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(200));

        var st = pcs.GetCurrentState();
        Assert.True(st.ActivePower > 0, "建压期应对电压差输出有功");
        Assert.True(st.ReactivePower > 0, "建压期应输出无功支撑");
    }

    [Fact]
    public void CurrentLimit_CapsPowerDuringRegulating()
    {
        var pcs = CreateDevice();
        pcs.ApplyBlackStartEnabled(true);
        pcs.ApplyIslandVoltageCommand(690);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.TransitionToGMode(GridMode.Islanded);
        pcs.SetTransformerMagnetizingReactiveKvar(800);
        pcs.SetBlackStartInrushDemand(50, 400);

        for (int i = 0; i < 3; i++)
            pcs.Update(1200, 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(200));

        var st = pcs.GetCurrentState();
        double sKva = Math.Sqrt(st.ActivePower * st.ActivePower + st.ReactivePower * st.ReactivePower);
        double iEst = sKva * 1000 / (Math.Max(st.AcVoltage, 10) * Math.Sqrt(3));
        Assert.True(iEst <= 1000 * 0.31);
    }
}

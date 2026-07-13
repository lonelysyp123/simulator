using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Xunit;

namespace EssSimulator.Tests.Devices;

public class PcsDisplayLabelsTests
{
    [Theory]
    [InlineData(OperationMode.Off, false, 0, false, 0, 1)]
    [InlineData(OperationMode.Off, true, 0, false, 3, 1)]
    [InlineData(OperationMode.Normal, false, 0, false, 0, 1)]
    [InlineData(OperationMode.Standby, true, 0, false, 0, 2)]
    [InlineData(OperationMode.Normal, true, 100, false, 0, 5)]
    [InlineData(OperationMode.Normal, true, -100, false, 0, 4)]
    [InlineData(OperationMode.Normal, true, 0, false, 0, 2)]
    [InlineData(OperationMode.Normal, true, 0, true, 0, 2)]
    [InlineData(OperationMode.Normal, true, 0, false, 1, 6)]
    public void ToOperationStatusCode_UsesProjectOperationStatusCodes(
        OperationMode mode, bool run, double powerKw, bool blackStart, ushort fault, int expected)
    {
        int code = PcsDisplayLabels.ToOperationStatusCode(mode, run, powerKw, blackStart, fault);
        Assert.Equal(expected, code);
    }

    [Fact]
    public void ToOperationStatusCode_FromState_MatchesGuiStopWithCommand()
    {
        var state = new PcsState
        {
            Mode = OperationMode.Off,
            FaultType = 2,
            ActivePower = 0
        };

        int code = PcsDisplayLabels.ToOperationStatusCode(state, externalRunCommand: true);
        Assert.Equal(1, code);
    }

    [Fact]
    public void MapPcsState_SyncsOperationStatusFromPhysicalState()
    {
        var src = new PcsState
        {
            Mode = OperationMode.Off,
            FaultType = 2,
            ActivePower = 0
        };
        var dst = new PcsData { pcsOnOffSwitch = true };

        PcsMapper.MapPcsState(src, dst, null!);

        Assert.Equal(1, dst.OperationStatus);
    }

    [Fact]
    public void WithdrawExternalRunCommand_ClearsCommandWithoutEmsStopSideEffects()
    {
        var pcs = CreateTestPcs();
        pcs.SyncExternalRunCommand(true);
        Assert.True(pcs.IsExternalRunCommand);

        pcs.WithdrawExternalRunCommand();

        Assert.False(pcs.IsExternalRunCommand);
    }

    [Fact]
    public void SyncRunCommandFeedback_ClearsDtoWhenDeviceWithdrewCommand()
    {
        var pcs = CreateTestPcs();
        pcs.SyncExternalRunCommand(true);
        pcs.WithdrawExternalRunCommand();
        var dto = new PcsData { pcsOnOffSwitch = true };

        PcsMapper.SyncRunCommandFeedback(pcs, dto);

        Assert.False(dto.pcsOnOffSwitch);
    }

    [Fact]
    public void FaultTrip_WithdrawsRunCommand()
    {
        var pcs = CreateTestPcs();
        pcs.SyncExternalRunCommand(true);
        pcs.TransitionToMode(OperationMode.Normal);
        pcs.GetCurrentState().FaultType = 3;
        pcs.GetCurrentState().FaultMessage = "test fault";

        pcs.Update(1300, isBmsFault: 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(200));

        Assert.Equal(OperationMode.Off, pcs.GetCurrentState().Mode);
        Assert.False(pcs.IsExternalRunCommand);
    }

    private static PcsDevice CreateTestPcs() =>
        PcsDeviceFactory.Create("pcs_test", new PcsDeviceConfig
        {
            AcNominalLineVoltageV = 690,
            FrequencyHz = 50,
            DcVoltageRangeMinV = 1000,
            DcVoltageRangeMaxV = 1500,
            MaxCurrentA = 1100,
            RatedPowerKw = 1250,
            MaxPowerKw = 1250,
            Efficiency = 0.99,
            GridLossCoefficient = 0.11,
            RampSlope = 500,
            RampIntervalMs = 100,
            RampDelayMs = 0,
        });
}

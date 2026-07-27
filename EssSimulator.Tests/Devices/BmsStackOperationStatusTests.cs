using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Xunit;

namespace EssSimulator.Tests.Devices;

public class BmsStackOperationStatusTests
{
    [Fact]
    public void Resolve_Normal_WhenLinkedMidSocNoAlarms()
    {
        var stack = CreateStack(soc: 0.5f, linked: true);

        Assert.Equal(BmsMapper.StackOpNormal, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_ChargeForbidden_WhenSocHigh()
    {
        var stack = CreateStack(soc: 0.95f, linked: true);

        Assert.Equal(0f, stack.MaxChargePower);
        Assert.True(stack.MaxDischargePower > 0);
        Assert.Equal(BmsMapper.StackOpChargeForbidden, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_DischargeForbidden_WhenSocLow()
    {
        var stack = CreateStack(soc: 0.05f, linked: true);

        Assert.Equal(0f, stack.MaxDischargePower);
        Assert.True(stack.MaxChargePower > 0);
        Assert.Equal(BmsMapper.StackOpDischargeForbidden, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_Standby_WhenNonChargeDischargeLevel2Alarm()
    {
        var stack = CreateStack(soc: 0.5f, linked: true);
        stack.Cluseter[0].Alarms.InsulationAlarm = true;

        Assert.True((stack.BMSAlarmSummary & BmsMapper.NonChargeDischargeAlarmMask) != 0);
        Assert.Equal(BmsMapper.StackOpStandby, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_NotStandby_WhenOnlyChargeDischargeLevel2Alarm()
    {
        var stack = CreateStack(soc: 0.5f, linked: true);
        stack.Cluseter[0].Alarms.ChargeOvercurrentAlarm = true;

        Assert.Equal(0, stack.BMSAlarmSummary & BmsMapper.NonChargeDischargeAlarmMask);
        Assert.Equal(BmsMapper.StackOpNormal, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_Shutdown_WhenLevel3Fault()
    {
        var stack = CreateStack(soc: 0.5f, linked: true);
        stack.Cluseter[0].Alarms.OvervoltageFault = true;

        Assert.True(stack.BMSFaultSummary > 0);
        Assert.Equal(BmsMapper.StackOpShutdown, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_Shutdown_WhenBmsUnlinked()
    {
        var stack = CreateStack(soc: 0.5f, linked: false);

        Assert.Equal(BmsMapper.StackOpShutdown, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void Resolve_Shutdown_TakesPriorityOverStandbyAndPowerLimits()
    {
        var stack = CreateStack(soc: 0.95f, linked: false);
        stack.Cluseter[0].Alarms.InsulationAlarm = true;

        Assert.Equal(BmsMapper.StackOpShutdown, BmsMapper.ResolveStackOperationStatus(stack));
    }

    [Fact]
    public void UpdateStackOperationStatus_WritesDtoField()
    {
        var bms = new BatteryManagementSystemData();
        bms.BatteryStacks.Add(CreateStack(soc: 0.5f, linked: true));

        BmsMapper.UpdateStackOperationStatus(bms);

        Assert.Equal(BmsMapper.StackOpNormal, bms.BatteryStacks[0].OperationStatus);
    }

    [Theory]
    [InlineData(0, "正常")]
    [InlineData(1, "禁充")]
    [InlineData(2, "禁放")]
    [InlineData(3, "待机")]
    [InlineData(4, "停机")]
    public void GetStackOperationStatusLabel_MapsCodes(int code, string expected)
    {
        Assert.Equal(expected, BmsMapper.GetStackOperationStatusLabel(code));
    }

    private static BatteryStack CreateStack(float soc, bool linked)
    {
        var stack = new BatteryStack
        {
            SOC = soc,
            IsPcsLinked = linked
        };
        stack.Cluseter.Add(new BatteryCluster());
        return stack;
    }
}

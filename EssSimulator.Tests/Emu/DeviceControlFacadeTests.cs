using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Model;
using EssSimulator.EssDeviceSimModel.Pv;
using EssSimulator.EssSimModelApi;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;

namespace EssSimulator.Tests.Emu;

/// <summary>
/// 内部设备直控门面测试：全程不注册 ModbusSimServer/点表，
/// 验证控制不依赖点位存在，状态经 DTO 镜像冒泡。
/// </summary>
public class DeviceControlFacadeTests
{
    private static PcsPhysicalConfig CreatePcsPhy() => new() { AcVoltageNominal = 690 };

    private static (EnergyStorageSystem ess, EnergyManagementData emu) BuildEssWithEmuMirror(
        SimulatorConfig? cfgOverride = null)
    {
        var cfg = cfgOverride ?? new SimulatorConfig
        {
            Devices =
            {
                new EssUnitConfig { Pcs = { new EssSimulator.Configuration.PcsDeviceConfig(), new EssSimulator.Configuration.PcsDeviceConfig() } }
            }
        };
        var pcsPhy = CreatePcsPhy();
        if (cfg.Devices.Count == 0)
            cfg.Devices.Add(new EssUnitConfig { Pcs = { new EssSimulator.Configuration.PcsDeviceConfig(), new EssSimulator.Configuration.PcsDeviceConfig() } });
        var ess = new EnergyStorageSystem(
            cfg,
            pcsPhy,
            new TransformerConfig(),
            new UnitTransformerConfig(),
            new LoadConfig(),
            new PccConfig(),
            new MeterConfig());

        var emu = PcsDataServer.BuildEmuMirror(cfg.Devices[0], pcsPhy);
        SimulatorHost.Instance.Register("ess", ess);
        SimulatorHost.Instance.Register("emu1", emu);
        return (ess, emu);
    }

    [Fact]
    public void PcsStartStop_WithoutPointMap_DrivesDeviceMode_AndBubblesDto()
    {
        var (ess, emu) = BuildEssWithEmuMirror();
        using (ess)
        {
            var pcs = ess._pcsList[0];

            Assert.True(DeviceControlFacade.TrySetPcsRun(1, true, out var startMsg));
            Assert.NotEqual(OperationMode.Off, pcs.GetCurrentState().Mode);
            Assert.True(emu.PcsList[0].pcsOnOffSwitch);
            Assert.NotEqual(1, emu.PcsList[0].OperationStatus);

            Assert.True(DeviceControlFacade.TrySetPcsRun(1, false, out var stopMsg));
            Assert.Equal(OperationMode.Off, pcs.GetCurrentState().Mode);
            Assert.False(emu.PcsList[0].pcsOnOffSwitch);
            Assert.Equal(1, emu.PcsList[0].OperationStatus);
        }
    }

    [Fact]
    public void PcsStart_WithGridAvailable_EntersNormal_AndPowerSettingReachesDto()
    {
        var (ess, emu) = BuildEssWithEmuMirror();
        using (ess)
        {
            var pcs = ess._pcsList[0];
            pcs.SetGridAvailable(true);

            Assert.True(DeviceControlFacade.TrySetPcsRun(1, true, out _));
            Assert.Equal(OperationMode.Normal, pcs.GetCurrentState().Mode);

            Assert.True(DeviceControlFacade.TrySetPcsPower(1, 500, 100, out var msg));
            Assert.Equal(500f, emu.PcsList[0].PCSActivePowerSetting);
            Assert.Equal(100f, emu.PcsList[0].PCSReactivePowerSetting);
            Assert.Equal(OperationMode.Normal, pcs.GetCurrentState().Mode);

            // 单项设定保留另一项现值
            Assert.True(DeviceControlFacade.TrySetPcsPower(1, activeKw: 300, reactiveKvar: null, out _));
            Assert.Equal(300f, emu.PcsList[0].PCSActivePowerSetting);
            Assert.Equal(100f, emu.PcsList[0].PCSReactivePowerSetting);
        }
    }

    [Fact]
    public void PcsStart_MainBreakerOpen_InterlockedToOff()
    {
        var (ess, emu) = BuildEssWithEmuMirror();
        using (ess)
        {
            var pcs = ess._pcsList[0];
            ess.SetMainBreakerClosed(false);

            Assert.True(DeviceControlFacade.TrySetPcsRun(1, true, out _));

            // 主断分闸且无黑启动：联锁强制停机，反馈同步清 DTO 启停位
            Assert.Equal(OperationMode.Off, pcs.GetCurrentState().Mode);
            Assert.False(emu.PcsList[0].pcsOnOffSwitch);
        }
    }

    [Fact]
    public void PcsStop_AfterFaultTrip_ClearsLatchedFault()
    {
        var (ess, emu) = BuildEssWithEmuMirror();
        using (ess)
        {
            var pcs = ess._pcsList[0];
            // 电网不可用 + Normal 下 Update 会产生真实故障锁存（孤岛检测），模拟运行中故障跳闸
            pcs.SyncExternalRunCommand(true);
            pcs.TransitionToMode(OperationMode.Normal);
            pcs.Update(1300, isBmsFault: 0, DateTime.UtcNow, TimeSpan.FromMilliseconds(200));

            Assert.Equal(OperationMode.Off, pcs.GetCurrentState().Mode);
            Assert.True(pcs.HasLatchedFaultTrip);

            // 写 0 停机路径清除故障锁存（与外部 EMS 重置语义一致）
            Assert.True(DeviceControlFacade.TrySetPcsRun(1, false, out _));
            Assert.False(pcs.HasLatchedFaultTrip);
            Assert.Equal(0, pcs.GetCurrentState().FaultType);
        }
    }

    [Fact]
    public void UnitBreaker_TogglesEssState_AndEmuMirror()
    {
        var (ess, emu) = BuildEssWithEmuMirror();
        using (ess)
        {
            Assert.True(DeviceControlFacade.TrySetUnitBreaker(1, false, out _));
            Assert.False(ess.IsUnitBreakerClosed(0));
            Assert.Equal(0, emu.Emu.PowerOnOff);

            Assert.True(DeviceControlFacade.TrySetUnitBreaker(1, true, out _));
            Assert.True(ess.IsUnitBreakerClosed(0));
            Assert.Equal(1, emu.Emu.PowerOnOff);
        }
    }

    [Fact]
    public void UnitBreaker_UnknownUnit_ReturnsFailure()
    {
        var (ess, _) = BuildEssWithEmuMirror();
        using (ess)
        {
            Assert.False(DeviceControlFacade.TrySetUnitBreaker(9, true, out var msg));
            Assert.Contains("emu9", msg);
        }
    }

    [Fact]
    public void PcsRun_UnknownPcs_ReturnsFailure()
    {
        var (ess, _) = BuildEssWithEmuMirror();
        using (ess)
        {
            // 单机组 2 PCS：pcs3 越界时布局回退末单元，槽位越界被拒
            Assert.False(DeviceControlFacade.TrySetPcsRun(3, true, out var msg));
            Assert.Contains("槽位越界", msg);
        }
    }

    [Fact]
    public void PvRunAndPower_DrivePvLogger_AndUnitMode()
    {
        var cfg = new SimulatorConfig
        {
            PvUnits =
            {
                new PvUnitRuntimeConfig
                {
                    Name = "光伏单元-1",
                    InverterCount = 1,
                    InverterRatedPowerKw = 320,
                    InverterMaxPowerKw = 352
                }
            }
        };
        var (ess, _) = BuildEssWithEmuMirror(cfg);
        using (ess)
        {
            var pv = ess.PvUnits[0];

            Assert.True(DeviceControlFacade.TrySetPvRun(1, true, out _));
            Assert.Equal(1, pv.Logger.SubarrayOnOff);
            Assert.Equal(OperationMode.Normal, pv.Inverters[0].GetCurrentState().Mode);

            Assert.True(DeviceControlFacade.TrySetPvPower(1, 100, 20, out _));
            Assert.Equal(100, pv.Logger.SubarrayActivePowerKw);
            Assert.Equal(20, pv.Logger.SubarrayReactivePowerKvar);

            // 超额定有功按额定 clamp
            Assert.True(DeviceControlFacade.TrySetPvPower(1, 9999, null, out _));
            Assert.Equal(pv.RatedPowerKw, pv.Logger.SubarrayActivePowerKw);

            Assert.True(DeviceControlFacade.TrySetPvRun(1, false, out _));
            Assert.Equal(0, pv.Logger.SubarrayOnOff);
            Assert.Equal(OperationMode.Off, pv.Inverters[0].GetCurrentState().Mode);
        }
    }
}

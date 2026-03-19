using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.EnergyManagementSystem.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// PCS/EMU 数据同步后台服务（100 ms 周期）。
    /// 具体映射逻辑委托给 <see cref="PcsMapper"/>。
    /// </summary>
    public class PcsDataServer : BackgroundService
    {
        private readonly EnergyManagementData _emuSys;

        public PcsDataServer(IOptions<PcsDefaultConfig> opts)
        {
            var cfg = opts.Value;
            _emuSys = new EnergyManagementData();

            for (int i = 1; i <= 2; i++)
            {
                var pcs = new PcsData { PcsId = i };
                ApplyDefaultConfig(pcs, cfg);
                _emuSys.PcsList.Add(pcs);
            }

            _emuSys.Emu.MaxChargePower    = cfg.EmuMaxChargePower;
            _emuSys.Emu.MaxDischargePower = cfg.EmuMaxDischargePower;
            SimulatorHost.Instance.Register("emu", _emuSys);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var store = SimulatorHost.Instance;
            EnergyStorageSystem? ess = null;

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ess ??= store.Get<EnergyStorageSystem>("ess");
                if (ess == null) continue;

                PcsMapper.MapPcsState(ess._pcs1.GetCurrentState(), _emuSys.PcsList[0], ess._batteryRack);
                PcsMapper.MapPcsState(ess._pcs2.GetCurrentState(), _emuSys.PcsList[1], ess._batteryRack2);
                PcsMapper.MapEmuState(_emuSys, ess._batteryRack);
                PcsMapper.ApplyEmuCommands(_emuSys, ess);
            }
        }

        private static void ApplyDefaultConfig(PcsData pcs, PcsDefaultConfig cfg)
        {
            pcs.BatteryChargeProtectionVoltage    = cfg.BatteryChargeProtectionVoltage;
            pcs.BatteryDischargeProtectionVoltage = cfg.BatteryDischargeProtectionVoltage;
            pcs.BatteryChargeProtectionCurrent    = cfg.BatteryChargeProtectionCurrent;
            pcs.BatteryDischargeProtectionCurrent = cfg.BatteryDischargeProtectionCurrent;
            pcs.BatteryChargeCurrentLimit         = cfg.BatteryChargeCurrentLimit;
            pcs.BatteryChargeVoltageLimit         = cfg.BatteryChargeVoltageLimit;
            pcs.BatteryDischargeCurrentLimit      = cfg.BatteryDischargeCurrentLimit;
            pcs.BatteryDischargeVoltageLimit      = cfg.BatteryDischargeVoltageLimit;
            pcs.BatteryChargePowerLimit           = cfg.BatteryChargePowerLimit;
            pcs.BatteryDischargePowerLimit        = cfg.BatteryDischargePowerLimit;
            pcs.ChargePowerLimit                  = cfg.ChargePowerLimit;
            pcs.DischargePowerLimit               = cfg.DischargePowerLimit;
            pcs.PCSRatePower                      = cfg.PCSRatePower;
            pcs.ActivePowerDispatchMode           = cfg.ActivePowerDispatchMode;
            pcs.ReactivePowerDispatchMode         = cfg.ReactivePowerDispatchMode;
            pcs.ActiveReactivePriority            = cfg.ActiveReactivePriority;
            pcs.FrequencyActiveSetting            = cfg.FrequencyActiveSetting;
        }
    }
}

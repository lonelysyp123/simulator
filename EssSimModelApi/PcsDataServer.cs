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
        private readonly List<EnergyManagementData> _emuUnits = new();
        private readonly int _unitCount;

        public PcsDataServer(IOptions<SimulatorConfig> simOpts, IOptions<PcsDefaultConfig> pcsOpts)
        {
            var cfg = pcsOpts.Value;
            _unitCount = Math.Max(1, simOpts.Value.Devices?.Count ?? 1);

            for (int u = 0; u < _unitCount; u++)
            {
                var emu = new EnergyManagementData();
                for (int i = 1; i <= 2; i++)
                {
                    var pcs = new PcsData { PcsId = i };
                    ApplyDefaultConfig(pcs, cfg);
                    emu.PcsList.Add(pcs);
                }

                emu.Emu.MaxChargePower    = cfg.EmuMaxChargePower;
                emu.Emu.MaxDischargePower = cfg.EmuMaxDischargePower;
                _emuUnits.Add(emu);
                SimulatorHost.Instance.Register($"emu{u + 1}", emu);
            }
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

                for (int u = 0; u < _unitCount; u++)
                {
                    int baseIdx = u * 2;
                    if (baseIdx + 1 >= ess._pcsList.Count || baseIdx + 1 >= ess._batteryRacks.Count) break;

                    var emu = _emuUnits[u];
                    PcsMapper.MapPcsState(ess._pcsList[baseIdx].GetCurrentState(), emu.PcsList[0], ess._batteryRacks[baseIdx]);
                    PcsMapper.MapPcsState(ess._pcsList[baseIdx + 1].GetCurrentState(), emu.PcsList[1], ess._batteryRacks[baseIdx + 1]);
                    PcsMapper.MapEmuState(emu, new[] { ess._batteryRacks[baseIdx], ess._batteryRacks[baseIdx + 1] });
                    PcsMapper.ApplyEmuCommands(emu, ess, baseIdx);
                }
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
            pcs.ActiveReactivePriority            = cfg.ActiveReactivePriority;
            pcs.FrequencyActiveSetting            = cfg.FrequencyActiveSetting;
        }
    }
}

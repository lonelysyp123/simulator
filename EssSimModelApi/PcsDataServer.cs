using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.EnergyManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// PCS/EMU 数据同步后台服务（100 ms 周期）：纯 ESS ↔ DTO，不含 Modbus。
    /// Modbus 读写由 DataExchangeSession 负责。
    /// </summary>
    public class PcsDataServer : BackgroundService
    {
        private readonly List<EnergyManagementData> _emuUnits = new();
        private readonly int _unitCount;
        private readonly IReadOnlyList<int> _pcsPerUnit;
        private readonly bool _autoStartPcsOnStartup;

        public PcsDataServer(
            IOptions<SimulatorConfig> simOpts,
            IOptions<PcsPhysicalConfig> pcsPhysicalOpts)
        {
            var sim = simOpts.Value;
            var pcsPhy = pcsPhysicalOpts.Value;
            _unitCount = sim.EffectiveEssUnitCount;
            _pcsPerUnit = sim.GetPcsCountsPerUnit();
            _autoStartPcsOnStartup = sim.Runtime.AutoStartPcsOnStartup;

            for (int u = 0; u < _unitCount; u++)
            {
                int pcsCount = u < _pcsPerUnit.Count ? _pcsPerUnit[u] : 2;
                var emu = new EnergyManagementData();
                for (int i = 1; i <= pcsCount; i++)
                {
                    var pcs = new PcsData { PcsId = i };
                    ApplyDefaultConfig(pcs, pcsPhy);
                    emu.PcsList.Add(pcs);
                }

                emu.Emu.MaxChargePower    = (float)(pcsCount * pcsPhy.MaxPower);
                emu.Emu.MaxDischargePower = (float)(pcsCount * pcsPhy.MaxPower);
                emu.Emu.PowerOnOff        = 1;
                _emuUnits.Add(emu);
                SimulatorHost.Instance.Register($"emu{u + 1}", emu);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var store = SimulatorHost.Instance;
            EnergyStorageSystem? ess = null;
            var startupBlackStartChecked = false;
            var startupPcsApplied = false;

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            int pcsBase = 0;
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ess ??= store.Get<EnergyStorageSystem>("ess");
                if (ess == null) continue;

                if (!startupBlackStartChecked)
                {
                    startupBlackStartChecked = true;
                    BlackStartSafety.ValidateAll(ess, "系统初始化");
                }

                if (_autoStartPcsOnStartup && !startupPcsApplied &&
                    PcsMapper.TryApplyStartupPcsStartStop(ess, _unitCount))
                {
                    startupPcsApplied = true;
                }

                pcsBase = 0;
                for (int u = 0; u < _unitCount; u++)
                {
                    PcsEmuSynchronizer.SyncUnit(ess, _emuUnits[u], u, pcsBase);
                    pcsBase += u < _pcsPerUnit.Count ? _pcsPerUnit[u] : 2;
                }
            }
        }

        private static void ApplyDefaultConfig(PcsData pcs, PcsPhysicalConfig pcsPhy)
        {
            pcs.BatteryChargePowerLimit           = (float)pcsPhy.MaxPower;
            pcs.BatteryDischargePowerLimit        = (float)pcsPhy.MaxPower;
            pcs.ChargePowerLimit                  = (float)pcsPhy.MaxPower;
            pcs.DischargePowerLimit               = (float)pcsPhy.MaxPower;
            pcs.PCSRatePower                      = (float)pcsPhy.RatedPower;
        }
    }
}

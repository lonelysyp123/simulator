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

        public PcsDataServer(
            IOptions<SimulatorConfig> simOpts,
            IOptions<PcsPhysicalConfig> pcsPhysicalOpts)
        {
            var pcsPhy = pcsPhysicalOpts.Value;
            _unitCount = Math.Max(1, simOpts.Value.Devices?.Count ?? 1);

            for (int u = 0; u < _unitCount; u++)
            {
                var emu = new EnergyManagementData();
                for (int i = 1; i <= 2; i++)
                {
                    var pcs = new PcsData { PcsId = i };
                    ApplyDefaultConfig(pcs, pcsPhy);
                    emu.PcsList.Add(pcs);
                }

                // 以 PCS 物理配置为唯一真源：EMU 单元级功率上限 = 2 路 PCS 的 MaxPower 之和
                emu.Emu.MaxChargePower    = (float)(2 * pcsPhy.MaxPower);
                emu.Emu.MaxDischargePower = (float)(2 * pcsPhy.MaxPower);
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

        private static void ApplyDefaultConfig(PcsData pcs, PcsPhysicalConfig pcsPhy)
        {
            // 以 PCS 配置统一：功率能力相关限值全部从 Pcs(MaxPower/RatedPower) 推导，避免与仿真不一致
            pcs.BatteryChargePowerLimit           = (float)pcsPhy.MaxPower;
            pcs.BatteryDischargePowerLimit        = (float)pcsPhy.MaxPower;
            pcs.ChargePowerLimit                  = (float)pcsPhy.MaxPower;
            pcs.DischargePowerLimit               = (float)pcsPhy.MaxPower;
            pcs.PCSRatePower                      = (float)pcsPhy.RatedPower;
        }
    }
}

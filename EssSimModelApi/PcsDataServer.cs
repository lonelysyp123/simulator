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
        private readonly double _unitXfRatedKw;

        public PcsDataServer(
            IOptions<SimulatorConfig> simOpts,
            IOptions<PcsPhysicalConfig> pcsPhysicalOpts,
            IOptions<UnitTransformerConfig> unitXfOpts)
        {
            var sim = simOpts.Value;
            var pcsPhy = pcsPhysicalOpts.Value;
            _unitXfRatedKw = unitXfOpts.Value.RatedPower;
            var units = sim.ResolveEssUnitsOrFallback();
            _unitCount = units.Count;
            _pcsPerUnit = sim.GetPcsCountsPerUnit();
            _autoStartPcsOnStartup = sim.Runtime.AutoStartPcsOnStartup;

            for (int u = 0; u < _unitCount; u++)
            {
                var emu = BuildEmuMirror(units[u], pcsPhy);
                _emuUnits.Add(emu);
                SimulatorHost.Instance.Register($"emu{u + 1}", emu);
            }
        }

        /// <summary>
        /// 构建单个机组的 EMU 协议镜像（扁平/分组两种构成）：
        /// 分组构成时组内 PcsList 与扁平 PcsList 共享同一 PcsData 实例引用，
        /// 并固定建 1 台单元变镜像（对应电气层单元变）。
        /// </summary>
        public static EnergyManagementData BuildEmuMirror(EssUnitConfig unit, PcsPhysicalConfig pcsPhy)
        {
            int pcsCount = unit.PcsCount;
            var emu = new EnergyManagementData();
            for (int i = 1; i <= pcsCount; i++)
            {
                var pcs = new PcsData { PcsId = i };
                ApplyDefaultConfig(pcs, pcsPhy);
                emu.PcsList.Add(pcs);
            }

            // 分组构成：组内 PcsList 与扁平 PcsList 共享同一 PcsData 实例引用
            if (unit.HasGroups)
            {
                int flatIndex = 0;
                foreach (var group in unit.Groups)
                {
                    var gd = new EmuGroupData { Name = group.Name };
                    for (int i = 0; i < group.PcsCount; i++)
                        gd.PcsList.Add(emu.PcsList[flatIndex + i]);
                    flatIndex += group.PcsCount;
                    if (!string.IsNullOrWhiteSpace(group.BreakerName))
                        gd.Breaker = new BreakerMirrorData { Closed = 1 };
                    // 组级电表镜像：按组态绑定顺序逐台建镜像（同组多表共母线，数值相同）
                    for (int i = 0; i < group.MeterNames.Count; i++)
                        gd.Meters.Add(new ElectricityMeterData());
                    emu.Groups.Add(gd);
                }
            }

            // 单元变镜像（对应电气层单元变，本期仅 1 台）
            emu.Transformers.Add(new TransformerMirrorData());

            emu.Emu.MaxChargePower    = (float)(pcsCount * pcsPhy.MaxPower);
            emu.Emu.MaxDischargePower = (float)(pcsCount * pcsPhy.MaxPower);
            emu.Emu.PowerOnOff        = 1;
            return emu;
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
                    PcsEmuSynchronizer.SyncUnit(ess, _emuUnits[u], u, pcsBase, _unitXfRatedKw);
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

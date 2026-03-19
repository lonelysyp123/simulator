using EssSimulator.EssDeviceSimModel;
using EssSimulator.Configuration;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// BMS 数据同步后台服务：以 100 ms 周期将物理模型数据同步到 BMS 接口数据对象。
    /// 按 UnitCount 动态创建 BMS 数据对象，不再硬编码两路。
    /// 具体映射逻辑全部委托给 <see cref="BmsMapper"/>。
    /// </summary>
    public class BmsDataService : BackgroundService
    {
        private readonly int _unitCount;
        private readonly BatteryManagementSystemData[] _bmsDataList;
        private readonly IReadOnlyList<int> _clusterCounts;

        public BmsDataService(SimulatorConfig cfg)
        {
            var bmsCfgList = cfg.GetBmsDeviceConfigs();
            _unitCount = bmsCfgList.Count;
            _bmsDataList = new BatteryManagementSystemData[_unitCount];
            _clusterCounts = bmsCfgList.Select(x => x.ClusterCount).ToList();

            var store = SimulatorHost.Instance;
            for (int i = 0; i < _unitCount; i++)
            {
                _bmsDataList[i] = BmsDataGenerator.GenerateSampleData(1, _clusterCounts[i]);
                store.Register($"bms{i + 1}", _bmsDataList[i]);
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

                var racks = ess._batteryRacks;

                for (int i = 0; i < _unitCount && i < racks.Count; i++)
                {
                    var rack    = racks[i];
                    var bmsData = _bmsDataList[i];

                    var rackState = rack.GetRackState();
                    BmsMapper.MapRackToStack(rackState, bmsData);
                    BmsMapper.SyncFaultToRack(bmsData, rackState);
                    BmsMapper.MapClusters(rack, bmsData);
                }

                _bmsDataList[0].Timestamp = DateTime.Now;
            }
        }

        // 保留供外部（Cmd.cs 等）直接调用的告警状态机（向后兼容）
        public void UpdateStateForUnder(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val)
            => BmsMapper.UpdateUnder(ref l1, ref l2, ref l3, t1, t2, t3, r1, r2, r3, val);

        public void UpdateStateForOver(ref bool? l1, ref bool? l2, ref bool? l3,
            float t1, float t2, float t3, float r1, float r2, float r3, double val)
            => BmsMapper.UpdateOver(ref l1, ref l2, ref l3, t1, t2, t3, r1, r2, r3, val);
    }
}

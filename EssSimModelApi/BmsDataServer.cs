using EssSimulator.EssDeviceSimModel;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;

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

        public BmsDataService(int unitCount, int clusterCount, int packCount)
        {
            _unitCount   = unitCount;
            _bmsDataList = new BatteryManagementSystemData[unitCount];

            var store = SimulatorHost.Instance;
            for (int i = 0; i < unitCount; i++)
            {
                _bmsDataList[i] = BmsDataGenerator.GenerateSampleData(1, clusterCount);
                store.Register($"bms{i + 1}", _bmsDataList[i]);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var store = SimulatorHost.Instance;
            EnergyStorageSystem? ess = null;

            // ESS 的 BatteryRack 列表：rack1 → index 0, rack2 → index 1, ...
            // 目前 EnergyStorageSystem 暴露 _batteryRack / _batteryRack2，
            // 此处通过数组访问，后续可扩展为 IReadOnlyList<BatteryRackSimulator>。
            BatteryRackSimulator?[] racks = new BatteryRackSimulator[_unitCount];

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ess ??= store.Get<EnergyStorageSystem>("ess");
                if (ess == null) continue;

                // 按索引填充 rack 引用（目前最多支持 2 路，与物理模型字段对应）
                if (_unitCount > 0) racks[0] = ess._batteryRack;
                if (_unitCount > 1) racks[1] = ess._batteryRack2;

                for (int i = 0; i < _unitCount; i++)
                {
                    var rack    = racks[i];
                    var bmsData = _bmsDataList[i];
                    if (rack == null) continue;

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

using EssSimulator.Configuration;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi.ElectricMeter;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// 电表数据同步后台服务；刷新周期与 ESS 主循环一致，映射逻辑委托给 <see cref="EmMapper"/>。
    /// </summary>
    public class EmDataService : BackgroundService
    {
        private readonly EmData _emData;
        private double _forwardKWh;
        private double _reverseKWh;

        public EmDataService()
        {
            _emData = EmDataGenerator.GenerateSampleData();
            SimulatorHost.Instance.Register("em", _emData);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var store = SimulatorHost.Instance;
            EnergyStorageSystem? ess = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                ess ??= store.Get<EnergyStorageSystem>("ess");
                int intervalMs = Math.Max(10, ess?.LoopIntervalMs ?? 100);
                await Task.Delay(intervalMs, stoppingToken);
                if (ess == null)
                    continue;

                EmMapper.MapEssToEmData(ess, _emData, TimeSpan.Zero,
                    ref _forwardKWh, ref _reverseKWh);
            }
        }
    }
}

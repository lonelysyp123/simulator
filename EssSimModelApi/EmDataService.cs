using EssSimulator.EssDeviceSimModel;
using EssSimulator.Core;
using EssSimulator.EssSimModelApi.ElectricMeter;
using EssSimulator.EssSimModelApi.Mappers;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// 电表数据同步后台服务（100 ms 周期）。
    /// 具体映射逻辑委托给 <see cref="EmMapper"/>。
    /// </summary>
    public class EmDataService : BackgroundService
    {
        private readonly EmData _emData;
        private double _forwardKWh;
        private double _reverseKWh;
        private DateTime? _lastUtc;

        public EmDataService()
        {
            _emData = EmDataGenerator.GenerateSampleData();
            SimulatorHost.Instance.Register("em", _emData);
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

                var now = DateTime.UtcNow;
                if (_lastUtc.HasValue)
                    EmMapper.MapEssToEmData(ess, _emData, now - _lastUtc.Value,
                        ref _forwardKWh, ref _reverseKWh);
                _lastUtc = now;
            }
        }
    }
}

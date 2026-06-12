using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.DataExchange.Config;
using EssSimulator.EssSimModelApi.Bms;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// BMS 堆级控制：一键并网 + 黑启动模式（纯 DTO/模型，Modbus 由 DataExchange 负责）。
    /// param11/12 写入时 <see cref="BmsLinkControlEffect"/> 即时触发，本服务周期扫描黑启动边沿。
    /// </summary>
    public class BmsLinkService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (SimulatorHost.Instance.Get<EnergyStorageSystem>("ess") == null)
                    continue;

                BmsLinkEngine.ApplyAllChannels();
            }
        }
    }
}

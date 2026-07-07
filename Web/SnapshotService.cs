using EssSimulator.Configuration;
using EssSimulator.Display;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace EssSimulator.Web
{
    /// <summary>周期采样仿真状态并通过 SignalR 推送（主接线/BMS/连接）。</summary>
    public sealed class SnapshotService : BackgroundService
    {
        private readonly IHubContext<RealtimeHub> _hub;
        private readonly SimulatorConfig _simCfg;
        private readonly WebConfig _webCfg;

        public SnapshotService(
            IHubContext<RealtimeHub> hub,
            IOptions<SimulatorConfig> simCfg,
            IOptions<WebConfig> webCfg)
        {
            _hub = hub;
            _simCfg = simCfg.Value;
            _webCfg = webCfg.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 等待仿真基本就绪
            while (!stoppingToken.IsCancellationRequested && !IsSimulatorReady())
            {
                try { await Task.Delay(500, stoppingToken); }
                catch (TaskCanceledException) { return; }
            }

            int intervalMs = Math.Max(200, _webCfg.SnapshotIntervalMs);
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try { await PushAll(stoppingToken); }
                    catch (Exception ex)
                    {
                        // 单次采样失败不影响后续
                        System.Diagnostics.Debug.WriteLine($"SnapshotService 采样失败: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private bool IsSimulatorReady()
        {
            var store = EssSimulator.Core.SimulatorHost.Instance;
            return store.Contains("ess") && store.Contains("simEm") && store.Contains("simBms1");
        }

        private async Task PushAll(CancellationToken ct)
        {
            // 主接线：推送 enriched 视图模型
            var mainLine = MainLineEnricher.Build();
            await _hub.Clients.Group(RealtimeChannels.MainLine)
                .SendAsync(RealtimeMethods.ReceiveMainLine, mainLine, ct);

            // 连接信息
            var conn = ConnectionSnapshotReader.Read();
            await _hub.Clients.Group(RealtimeChannels.Connections)
                .SendAsync(RealtimeMethods.ReceiveConnections, conn, ct);

            // 电池舱总览：每个通道推送一次
            int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            for (int i = 0; i < unitCount; i++)
            {
                try
                {
                    var overview = BatterySnapshotReader.ReadOverview(i);
                    await _hub.Clients.Group($"{RealtimeChannels.Battery}.{i + 1}")
                        .SendAsync(RealtimeMethods.ReceiveBattery, overview, ct);
                }
                catch { /* 单个舱失败不影响其他 */ }
            }

            // 告警状态（仅当活跃时推送，减少流量）
            var alert = FatalSystemAlert.GetSnapshot();
            if (alert.IsActive)
            {
                await _hub.Clients.All.SendAsync(RealtimeMethods.ReceiveAlert, alert, ct);
            }
        }
    }
}

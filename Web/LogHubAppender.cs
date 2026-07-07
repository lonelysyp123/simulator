using System.Collections.Concurrent;
using log4net.Appender;
using log4net.Core;
using Microsoft.AspNetCore.SignalR;

namespace EssSimulator.Web
{
    /// <summary>log4net Appender：把日志事件转为 DTO 入队，由 LogHubDispatcher 推送到 SignalR。</summary>
    public sealed class LogHubAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null) return;
            try
            {
                var dto = new LogEntryDto
                {
                    Timestamp = loggingEvent.TimeStamp,
                    Level = loggingEvent.Level?.Name ?? "",
                    Logger = loggingEvent.LoggerName ?? "",
                    Message = loggingEvent.RenderedMessage ?? "",
                    Thread = loggingEvent.ThreadName,
                    Exception = loggingEvent.ExceptionObject?.ToString()
                };
                LogHubBridge.Enqueue(dto);
            }
            catch
            {
                // 日志桥接失败不可影响主流程
            }
        }
    }

    /// <summary>静态桥接：Appender 入队，Dispatcher 出队并推送。</summary>
    internal static class LogHubBridge
    {
        private static readonly ConcurrentQueue<LogEntryDto> Queue = new();
        private static readonly int MaxQueue = 2000;

        public static void Enqueue(LogEntryDto entry)
        {
            Queue.Enqueue(entry);
            // 简单背压：超出上限丢弃最旧
            while (Queue.Count > MaxQueue && Queue.TryDequeue(out _)) { }
        }

        public static bool TryDequeue(out LogEntryDto entry) => Queue.TryDequeue(out entry!);
    }

    /// <summary>后台服务：消费 LogHubBridge 队列，通过 SignalR 推送日志到 logs 频道。</summary>
    public sealed class LogHubDispatcher : BackgroundService
    {
        private readonly IHubContext<RealtimeHub> _hub;
        public LogHubDispatcher(IHubContext<RealtimeHub> hub) => _hub = hub;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                while (LogHubBridge.TryDequeue(out var entry))
                {
                    try
                    {
                        await _hub.Clients.Group(RealtimeChannels.Logs)
                            .SendAsync(RealtimeMethods.ReceiveLog, entry, stoppingToken);
                    }
                    catch { /* 忽略推送失败 */ }
                }

                try { await Task.Delay(150, stoppingToken); }
                catch (TaskCanceledException) { break; }
            }
        }
    }
}

using System.Threading;
using log4net;

namespace EssSimulator.Display
{
    /// <summary>
    /// 全进程级严重故障提示：黑启动联锁违规等安全事件。
    /// B/S 改造后不再渲染控制台 overlay，改为触发事件供 Web 层（SignalR）向前端推送全屏告警，
    /// 并在倒计时后请求应用关闭。
    /// </summary>
    public static class FatalSystemAlert
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FatalSystemAlert));
        private static int _triggered;
        private static string _message = "";
        private static string _detail = "";
        private static DateTime _exitAtUtc;

        /// <summary>告警触发时的事件，订阅者收到后通过 SignalR 推送前端。</summary>
        public static event Action<FatalAlertEventArgs>? AlertTriggered;

        public static bool IsActive => Volatile.Read(ref _triggered) == 1;

        public static string Message => _message;
        public static string Detail => _detail;

        /// <summary>触发严重故障：记录信息、通知订阅者、并安排进程退出倒计时（仅首次生效）。</summary>
        public static void Trigger(string message, string detail, TimeSpan exitDelay)
        {
            if (Interlocked.CompareExchange(ref _triggered, 1, 0) != 0)
                return;

            _message = message;
            _detail = detail;
            _exitAtUtc = DateTime.UtcNow + exitDelay;

            try
            {
                AlertTriggered?.Invoke(new FatalAlertEventArgs(message, detail, exitDelay));
            }
            catch (Exception ex)
            {
                Log.Debug("FatalSystemAlert 订阅者通知异常", ex);
            }
        }

        public static int SecondsUntilExit =>
            IsActive ? Math.Max(0, (int)Math.Ceiling((_exitAtUtc - DateTime.UtcNow).TotalSeconds)) : 0;

        public static FatalAlertSnapshot GetSnapshot() => new()
        {
            IsActive = IsActive,
            Message = _message,
            Detail = _detail,
            SecondsUntilExit = SecondsUntilExit
        };

        public static void ForceExitProcess()
        {
            Log.Fatal("严重故障：执行进程退出。");
            Environment.Exit(1);
        }
    }

    public sealed class FatalAlertEventArgs : EventArgs
    {
        public string Message { get; }
        public string Detail { get; }
        public TimeSpan ExitDelay { get; }

        public FatalAlertEventArgs(string message, string detail, TimeSpan exitDelay)
        {
            Message = message;
            Detail = detail;
            ExitDelay = exitDelay;
        }
    }

    public sealed class FatalAlertSnapshot
    {
        public bool IsActive { get; set; }
        public string Message { get; set; } = "";
        public string Detail { get; set; } = "";
        public int SecondsUntilExit { get; set; }
    }
}

using System.Threading;
using log4net;
using Spectre.Console;

namespace EssSimulator.Display
{
    /// <summary>
    /// 全进程级严重故障提示：任意界面居中红框显示，倒计时后强制退出整个系统。
    /// </summary>
    public static class FatalSystemAlert
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FatalSystemAlert));
        private static int _triggered;
        private static string _message = "";
        private static string _detail = "";
        private static DateTime _exitAtUtc;
        private static Thread? _overlayThread;

        public static bool IsActive => Volatile.Read(ref _triggered) == 1;

        /// <summary>触发严重故障 UI 与进程退出倒计时（仅首次生效）。</summary>
        public static void Trigger(string message, string detail, TimeSpan exitDelay)
        {
            if (Interlocked.CompareExchange(ref _triggered, 1, 0) != 0)
                return;

            _message = message;
            _detail = detail;
            _exitAtUtc = DateTime.UtcNow + exitDelay;

            GuiMain.ActivateFatalShutdown();
            StartOverlayThread();
        }

        /// <summary>各界面循环中调用：显示居中红框并阻止正常交互。</summary>
        /// <returns>true 表示已进入严重故障模式，调用方应跳过常规 UI。</returns>
        public static bool PollFatalUi()
        {
            if (!IsActive)
                return false;

            RenderOverlay();

            if (DateTime.UtcNow >= _exitAtUtc)
                ForceExitProcess();

            return true;
        }

        public static int SecondsUntilExit =>
            IsActive ? Math.Max(0, (int)Math.Ceiling((_exitAtUtc - DateTime.UtcNow).TotalSeconds)) : 0;

        private static void StartOverlayThread()
        {
            _overlayThread = new Thread(() =>
            {
                while (IsActive && DateTime.UtcNow < _exitAtUtc)
                {
                    try { RenderOverlay(); }
                    catch (Exception ex) { Log.Debug("严重故障 overlay 渲染失败（终端受限）", ex); }
                    Thread.Sleep(250);
                }

                if (IsActive)
                    ForceExitProcess();
            })
            { IsBackground = true, Name = "FatalSystemAlertOverlay" };
            _overlayThread.Start();
        }

        public static void RenderOverlay()
        {
            int seconds = SecondsUntilExit;
            var body = new Markup(
                $"[bold red]严重故障[/]\n\n" +
                $"{EscapeMarkup(_message)}\n\n" +
                (string.IsNullOrWhiteSpace(_detail) ? "" : $"{EscapeMarkup(_detail)}\n\n") +
                $"[red]{seconds} 秒后退出整个系统…[/]\n" +
                "[dim]安全联锁已触发，请勿继续操作。[/]");

            var panel = new Panel(body)
                .Header("[red]■ 安全联锁 ■[/]", Justify.Center)
                .Border(BoxBorder.Double)
                .BorderColor(Color.Red)
                .Padding(2, 1);

            Console.Clear();
            AnsiConsole.Write(new Align(panel, HorizontalAlignment.Center, VerticalAlignment.Middle));
        }

        public static void ForceExitProcess()
        {
            try { Console.Clear(); }
            catch (Exception ex) { Log.Debug("退出前 Console.Clear 失败", ex); }
            Environment.Exit(1);
        }

        private static string EscapeMarkup(string text) =>
            Markup.Escape(text ?? string.Empty);
    }
}

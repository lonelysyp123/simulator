using System.Threading;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.Display;
using log4net;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// 黑启动联锁违规时的进程级安全响应（告警 + 退出）。
    /// 联锁判定逻辑在 <see cref="BlackStartInterlock"/>（设备层）。
    /// </summary>
    public static class BlackStartSafety
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BlackStartSafety));
        private static IHostApplicationLifetime? _appLifetime;
        private static int _shutdownScheduled;

        public static void Register(IHostApplicationLifetime lifetime) =>
            _appLifetime = lifetime;

        /// <summary>扫描全部 PCS（初始化或周期检查）。</summary>
        public static void ValidateAll(EnergyStorageSystem ess, string trigger)
        {
            ess.ValidatePcsBlackStartInterlocks(pcsNumber =>
                ReportViolation(trigger, pcsNumber));
        }

        /// <summary>开启黑启动前检查；若违规则禁止开启并触发退出流程。</summary>
        /// <returns>true 表示允许开启黑启动</returns>
        public static bool TryEnableBlackStart(EnergyStorageSystem ess, int pcsSimIndex, bool requested)
        {
            if (!requested)
                return true;

            if (ess.TrySetPcsBlackStart(pcsSimIndex, true))
                return true;

            ReportViolation("开启黑启动", pcsSimIndex + 1);
            return false;
        }

        public static void ReportViolation(string trigger, int pcsNumber) =>
            TriggerStationShortCircuitShutdown(trigger, pcsNumber);

        private static void TriggerStationShortCircuitShutdown(string trigger, int pcsNumber)
        {
            if (Interlocked.CompareExchange(ref _shutdownScheduled, 1, 0) != 0)
                return;

            const string message =
                "电站短路风险：主断路器与单元高压断路器均为合闸时禁止黑启动。";
            var detail = $"触发来源：{trigger}（PCS #{pcsNumber}）";
            Log.Fatal($"【严重】{message} {detail}");

            FatalSystemAlert.Trigger(message, detail, TimeSpan.FromSeconds(5));

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                Log.Fatal("黑启动联锁：执行进程退出。");
                try { _appLifetime?.StopApplication(); }
                catch (Exception ex) { Log.Warn("黑启动联锁 StopApplication 失败", ex); }
                FatalSystemAlert.ForceExitProcess();
            });
        }
    }
}

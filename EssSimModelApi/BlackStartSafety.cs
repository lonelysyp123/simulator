using System.Threading;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.Display;
using log4net;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// 黑启动与断路器联锁：仅当「主断合闸 + 所属单元高压合闸」时开启黑启动视为向电网侧建压短路风险。
    /// 真实顺序（主断分模拟失电 → 单元高压合 → 黑启动）不受本联锁限制。
    /// </summary>
    public static class BlackStartSafety
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BlackStartSafety));
        private static IHostApplicationLifetime? _appLifetime;
        private static int _shutdownScheduled;

        public static void Register(IHostApplicationLifetime lifetime) =>
            _appLifetime = lifetime;

        /// <summary>
        /// 该路 PCS 黑启动时，主断与所属单元高压断均合闸 → 向电网侧建压，存在短路风险。
        /// </summary>
        public static bool IsStationShortCircuit(EnergyStorageSystem ess, int pcsSimIndex, bool blackStartEnabled)
        {
            if (!blackStartEnabled)
                return false;

            if (!ess._breaker.IsClosed)
                return false;

            int unit = pcsSimIndex / 2;
            if (unit < 0 || unit >= ess._unitBreakers.Count)
                return false;

            return ess._unitBreakers[unit].IsClosed;
        }

        /// <summary>扫描全部 PCS（初始化或周期检查）。</summary>
        public static void ValidateAll(EnergyStorageSystem ess, string trigger)
        {
            for (int i = 0; i < ess._pcsList.Count; i++)
            {
                var enabled = ess._pcsList[i].GetCurrentState().BlackStartEnabled;
                if (IsStationShortCircuit(ess, i, enabled))
                {
                    TriggerStationShortCircuitShutdown(trigger, i + 1);
                    return;
                }
            }
        }

        /// <summary>开启黑启动前检查；若违规则禁止开启并触发退出流程。</summary>
        /// <returns>true 表示允许开启黑启动</returns>
        public static bool TryEnableBlackStart(EnergyStorageSystem ess, int pcsSimIndex, bool requested)
        {
            if (!requested)
                return true;

            if (!IsStationShortCircuit(ess, pcsSimIndex, true))
                return true;

            TriggerStationShortCircuitShutdown("开启黑启动", pcsSimIndex + 1);
            return false;
        }

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
                try { _appLifetime?.StopApplication(); } catch { /* ignore */ }
                FatalSystemAlert.ForceExitProcess();
            });
        }
    }
}

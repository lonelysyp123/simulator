using EssSimulator.Configuration;
using EssSimulator.Core;
using log4net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// LocalControl 协议桥接服务：
    /// - 上行：从 simEmuX 采集协议点，汇总写入 simLcX 的遥测点；
    /// - 下行：监听 simLcX 控制点变化，分发写入对应 simEmuX 控制点。
    /// 全程仅通过协议模拟对象交互，不直接触碰物理仿真对象。
    /// </summary>
    public class LocalControlBridgeService : BackgroundService
    {
        private readonly SimulatorConfig _cfg;
        private readonly Dictionary<string, double> _controlShadow = new();
        private readonly ILog _log = LogManager.GetLogger(typeof(LocalControlBridgeService));

        public LocalControlBridgeService(IOptions<SimulatorConfig> simOptions)
        {
            _cfg = simOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_cfg.Protocol.EnableLocalControl)
                return;

            var store = SimulatorHost.Instance;
            int emuPerGroup = Math.Max(1, _cfg.Protocol.LocalControlEmuPerGroup);
            int emuCount = Math.Max(1, _cfg.Devices?.Count ?? 1);
            int lcCount = (int)Math.Ceiling(emuCount / (double)emuPerGroup);

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                for (int lcIdx = 0; lcIdx < lcCount; lcIdx++)
                {
                    string lcName = $"simLc{lcIdx + 1}";
                    var lc = store.Get<ModbusSimServer>(lcName);
                    if (lc == null || !lc.IsOnline) continue;

                    try
                    {
                        SyncTelemetryForLc(store, lc, lcIdx, emuPerGroup, emuCount);
                        ApplyLcControlsToEmu(store, lc, lcName, lcIdx, emuPerGroup, emuCount);
                    }
                    catch
                    {
                        // 协议尚未就绪或单次读写失败时忽略本周期，避免后台服务异常导致整机退出。
                    }
                }
            }
        }

        private void SyncTelemetryForLc(
            SimulatorHost store,
            ModbusSimServer lc,
            int lcIdx,
            int emuPerGroup,
            int emuCount)
        {
            int startEmu = lcIdx * emuPerGroup;

            // param1: 系统故障总（任一 PCS 故障）
            bool anyFault = false;
            // param2: 黑启动模式状态（8 台均开启=1）
            bool allBlackStart = true;
            // param3: 高压断路器状态（AA=开；EE=合）
            bool allHvClosed = true;

            for (int localPcs = 0; localPcs < 8; localPcs++)
            {
                int emuOffset = localPcs / 2;
                int emuIndex = startEmu + emuOffset;
                if (emuIndex >= emuCount)
                {
                    WritePcsTelemetryDefaults(lc, localPcs);
                    allBlackStart = false;
                    allHvClosed = false;
                    continue;
                }

                string emuName = $"simEmu{emuIndex + 1}";
                var emu = store.Get<ModbusSimServer>(emuName);
                if (emu == null || !emu.IsOnline)
                {
                    WritePcsTelemetryDefaults(lc, localPcs);
                    allBlackStart = false;
                    allHvClosed = false;
                    continue;
                }

                bool slot0 = (localPcs % 2) == 0;
                string faultParam = slot0 ? "param27" : "param54";
                string alarmParam = slot0 ? "param26" : "param53";
                string statusParam = slot0 ? "param25" : "param52";
                string freqParam = slot0 ? "param4" : "param31";
                string vabParam = slot0 ? "param1" : "param28";
                string vbcParam = slot0 ? "param2" : "param29";
                string vcaParam = slot0 ? "param3" : "param30";
                string blackStartParam = slot0 ? "pcs1_blackstart_enable" : "pcs2_blackstart_enable";

                int faultLc = localPcs < 4 ? 4 + localPcs : 32 + (localPcs - 4);
                int alarmLc = localPcs < 4 ? 8 + localPcs : 36 + (localPcs - 4);
                int statusLc = localPcs < 4 ? 12 + localPcs : 40 + (localPcs - 4);
                int freqLc = localPcs < 4 ? 16 + localPcs : 44 + (localPcs - 4);
                int vBaseLc = (localPcs < 4 ? 20 : 48) + (localPcs % 4) * 3;

                var faultVal = ReadParamOrDefault(emu, faultParam);
                var alarmVal = ReadParamOrDefault(emu, alarmParam);
                var statusVal = ReadParamOrDefault(emu, statusParam);
                var freqVal = ReadParamOrDefault(emu, freqParam);
                var vabVal = ReadParamOrDefault(emu, vabParam);
                var vbcVal = ReadParamOrDefault(emu, vbcParam);
                var vcaVal = ReadParamOrDefault(emu, vcaParam);
                var blackStartVal = ReadParamOrDefault(emu, blackStartParam);
                var hvBreakerVal = ReadParamOrDefault(emu, "highvoltagebreakeronoff");

                anyFault |= ToDouble(faultVal) != 0;
                allBlackStart &= ToDouble(blackStartVal) != 0;
                allHvClosed &= ToDouble(hvBreakerVal) != 0;

                lc.SetDataStoreByMesurePointName($"param{faultLc}", faultVal);
                lc.SetDataStoreByMesurePointName($"param{alarmLc}", alarmVal);
                lc.SetDataStoreByMesurePointName($"param{statusLc}", statusVal);
                lc.SetDataStoreByMesurePointName($"param{freqLc}", freqVal);
                lc.SetDataStoreByMesurePointName($"param{vBaseLc}", vabVal);
                lc.SetDataStoreByMesurePointName($"param{vBaseLc + 1}", vbcVal);
                lc.SetDataStoreByMesurePointName($"param{vBaseLc + 2}", vcaVal);
            }

            lc.SetDataStoreByMesurePointName("param1", anyFault ? 1 : 0);
            lc.SetDataStoreByMesurePointName("param2", allBlackStart ? 1 : 0);
            lc.SetDataStoreByMesurePointName("param3", allHvClosed ? 0xEE : 0xAA);
        }

        private void ApplyLcControlsToEmu(
            SimulatorHost store,
            ModbusSimServer lc,
            string lcName,
            int lcIdx,
            int emuPerGroup,
            int emuCount)
        {
            int startEmu = lcIdx * emuPerGroup;

            for (int localPcs = 0; localPcs < 8; localPcs++)
            {
                int emuOffset = localPcs / 2;
                int emuIndex = startEmu + emuOffset;
                if (emuIndex >= emuCount) continue;

                string emuName = $"simEmu{emuIndex + 1}";
                var emu = store.Get<ModbusSimServer>(emuName);
                if (emu == null || !emu.IsOnline) continue;

                bool slot0 = (localPcs % 2) == 0;
                string startStopTarget = slot0 ? "pcs1_startstop" : "pcs2_startstop";
                string pTarget = slot0 ? "param55" : "param59";
                string qTarget = slot0 ? "param56" : "param60";
                string islandVTarget = slot0 ? "param64" : "param65";

                int startStopLc = localPcs < 4 ? 60 + localPcs : 80 + (localPcs - 4);
                int pLc = localPcs < 4 ? 64 + localPcs : 84 + (localPcs - 4);
                int qLc = localPcs < 4 ? 68 + localPcs : 88 + (localPcs - 4);
                int islandVLc = localPcs < 4 ? 72 + localPcs : 92 + (localPcs - 4);

                ForwardWhenChanged(lc, lcName, $"param{startStopLc}", emu, startStopTarget, asBool: true);
                ForwardWhenChanged(lc, lcName, $"param{pLc}", emu, pTarget, asBool: false);
                ForwardWhenChanged(lc, lcName, $"param{qLc}", emu, qTarget, asBool: false);
                ForwardWhenChanged(lc, lcName, $"param{islandVLc}", emu, islandVTarget, asBool: false);
            }

            // param100: 黑启动模式（全局） -> 全部 emu 的 PCS1/PCS2 黑启动开关
            PrimeGlobalBlackStartShadow(store, lc, lcName, startEmu, emuPerGroup, emuCount);
            if (TryReadChangedControl(lc, lcName, "param100", asBool: true, out var prevBlackStart, out var blackStartGlobal))
            {
                _log.Info(
                    $"[LC-Change] {lcName}.param100: {FormatControlValue(prevBlackStart)} -> {FormatControlValue(blackStartGlobal)}");
                bool success = true;
                int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
                for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
                {
                    var emu = store.Get<ModbusSimServer>($"simEmu{emuIndex + 1}");
                    if (emu == null || !emu.IsOnline) continue;
                    success &= TryPublishControlWithLog(
                        emu,
                        "pcs1_blackstart_enable",
                        blackStartGlobal,
                        asBool: true,
                        sourceLabel: $"{lcName}.param100");
                    success &= TryPublishControlWithLog(
                        emu,
                        "pcs2_blackstart_enable",
                        blackStartGlobal,
                        asBool: true,
                        sourceLabel: $"{lcName}.param100");
                }

                if (success)
                    UpdateControlShadow(lcName, "param100", blackStartGlobal);
            }

            // param101: 高压断路器开合（全局）
            // 仅接受 170(0xAA, 开) / 238(0xEE, 合)；其它值忽略且不下发。
            PrimeGlobalHvBreakerShadow(store, lc, lcName, startEmu, emuPerGroup, emuCount);
            if (TryReadChangedControl(lc, lcName, "param101", asBool: false, out var prevHvCmd, out var hvRawCmd))
            {
                if (!TryNormalizeHvBreakerCommand(hvRawCmd, out var hvCmd, out var hvClosed))
                {
                    if (!double.IsNaN(prevHvCmd))
                    {
                        try { lc.SetDataStoreByMesurePointName("param101", prevHvCmd); }
                        catch { /* 忽略回退失败，后续周期继续尝试 */ }
                    }
                    return;
                }

                _log.Info(
                    $"[LC-Change] {lcName}.param101: {FormatControlValue(prevHvCmd)} -> {FormatControlValue(hvCmd)}");
                bool success = true;
                int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
                for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
                {
                    var emu = store.Get<ModbusSimServer>($"simEmu{emuIndex + 1}");
                    if (emu == null || !emu.IsOnline) continue;
                    success &= TryPublishControlWithLog(
                        emu,
                        "highvoltagebreakeronoff",
                        hvClosed ? 1 : 0,
                        asBool: true,
                        sourceLabel: $"{lcName}.param101");
                }

                if (success)
                    UpdateControlShadow(lcName, "param101", hvCmd);
            }
        }

        private static void WritePcsTelemetryDefaults(ModbusSimServer lc, int localPcs)
        {
            int faultLc = localPcs < 4 ? 4 + localPcs : 32 + (localPcs - 4);
            int alarmLc = localPcs < 4 ? 8 + localPcs : 36 + (localPcs - 4);
            int statusLc = localPcs < 4 ? 12 + localPcs : 40 + (localPcs - 4);
            int freqLc = localPcs < 4 ? 16 + localPcs : 44 + (localPcs - 4);
            int vBaseLc = (localPcs < 4 ? 20 : 48) + (localPcs % 4) * 3;

            lc.SetDataStoreByMesurePointName($"param{faultLc}", 0);
            lc.SetDataStoreByMesurePointName($"param{alarmLc}", 0);
            lc.SetDataStoreByMesurePointName($"param{statusLc}", 0);
            lc.SetDataStoreByMesurePointName($"param{freqLc}", 0);
            lc.SetDataStoreByMesurePointName($"param{vBaseLc}", 0);
            lc.SetDataStoreByMesurePointName($"param{vBaseLc + 1}", 0);
            lc.SetDataStoreByMesurePointName($"param{vBaseLc + 2}", 0);
        }

        private static object ReadParamOrDefault(ModbusSimServer server, string paramName)
        {
            try
            {
                return server.GetDataObjectByMesurePointName(paramName) ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private void ForwardWhenChanged(
            ModbusSimServer lc,
            string lcName,
            string lcParam,
            ModbusSimServer targetEmu,
            string targetParam,
            bool asBool)
        {
            if (!TryReadChangedControl(lc, lcName, lcParam, asBool, out var prevValue, out var currentValue))
                return;

            _log.Info(
                $"[LC-Change] {lcName}.{lcParam}: {FormatControlValue(prevValue)} -> {FormatControlValue(currentValue)}");

            bool success;
            if (double.IsNaN(prevValue))
            {
                // 初始化阶段：仅在 LC 与 EMU 当前值不一致时下发，避免把初值当作有效控制命令。
                var targetValue = ToDouble(ReadParamOrDefault(targetEmu, targetParam));
                targetValue = asBool ? (targetValue != 0 ? 1 : 0) : targetValue;
                success = Math.Abs(targetValue - currentValue) < 1e-9 ||
                          TryPublishControlWithLog(
                              targetEmu,
                              targetParam,
                              currentValue,
                              asBool,
                              sourceLabel: $"{lcName}.{lcParam}");
            }
            else
            {
                success = TryPublishControlWithLog(
                    targetEmu,
                    targetParam,
                    currentValue,
                    asBool,
                    sourceLabel: $"{lcName}.{lcParam}");
            }

            // 关键：只有下发成功后才推进 shadow，失败时保持旧值，后续周期可重试。
            if (success)
                UpdateControlShadow(lcName, lcParam, currentValue);
        }

        private void PrimeGlobalBlackStartShadow(
            SimulatorHost store,
            ModbusSimServer lc,
            string lcName,
            int startEmu,
            int emuPerGroup,
            int emuCount)
        {
            const string lcParam = "param100";
            string key = $"{lcName}:{lcParam}";
            if (_controlShadow.ContainsKey(key))
                return;

            bool enabled = true;
            int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
            for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
            {
                var emu = store.Get<ModbusSimServer>($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                {
                    enabled = false;
                    break;
                }

                enabled &= ToDouble(ReadParamOrDefault(emu, "pcs1_blackstart_enable")) != 0;
                enabled &= ToDouble(ReadParamOrDefault(emu, "pcs2_blackstart_enable")) != 0;
            }

            double val = enabled ? 1 : 0;
            _controlShadow[key] = val;
            try
            {
                lc.SetDataStoreByMesurePointName(lcParam, val);
            }
            catch
            {
                // 忽略首轮对齐写失败，后续周期会再次尝试。
            }
        }

        private void PrimeGlobalHvBreakerShadow(
            SimulatorHost store,
            ModbusSimServer lc,
            string lcName,
            int startEmu,
            int emuPerGroup,
            int emuCount)
        {
            const string lcParam = "param101";
            string key = $"{lcName}:{lcParam}";
            if (_controlShadow.ContainsKey(key))
                return;

            bool closed = true;
            int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
            for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
            {
                var emu = store.Get<ModbusSimServer>($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                {
                    closed = false;
                    break;
                }

                closed &= ToDouble(ReadParamOrDefault(emu, "highvoltagebreakeronoff")) != 0;
            }

            double val = closed ? 0xEE : 0xAA;
            _controlShadow[key] = val;
            try
            {
                lc.SetDataStoreByMesurePointName(lcParam, val);
            }
            catch
            {
                // 忽略首轮对齐写失败，后续周期会再次尝试。
            }
        }

        private bool TryReadChangedControl(
            ModbusSimServer lc,
            string lcName,
            string param,
            bool asBool,
            out double previousValue,
            out double currentValue)
        {
            previousValue = 0;
            currentValue = 0;
            object? raw;
            try
            {
                raw = lc.GetDataObjectByMesurePointName(param);
            }
            catch
            {
                return false;
            }
            if (raw == null)
                return false;

            currentValue = ToDouble(raw);
            currentValue = asBool ? (currentValue != 0 ? 1 : 0) : currentValue;

            string key = $"{lcName}:{param}";

            // 初始化也纳入变更链路：首次视为从 shadow/未初始化 到当前值。
            if (!_controlShadow.TryGetValue(key, out var prev))
            {
                previousValue = double.NaN;
                return true;
            }

            previousValue = prev;
            if (Math.Abs(prev - currentValue) < 1e-9)
                return false;

            return true;
        }

        private void UpdateControlShadow(string lcName, string lcParam, double value)
        {
            _controlShadow[$"{lcName}:{lcParam}"] = value;
        }

        private bool TryPublishControlWithLog(
            ModbusSimServer targetEmu,
            string targetParam,
            double value,
            bool asBool,
            string sourceLabel)
        {
            try
            {
                if (asBool)
                    targetEmu.PublishControlToSlave(targetParam, value != 0);
                else
                    targetEmu.PublishControlToSlave(targetParam, value);
            }
            catch (Exception ex)
            {
                _log.Error(
                    $"[LC-Publish:failed] {sourceLabel} -> {targetEmu}.{targetParam} value={value} error={ex.Message}",
                    ex);
                return false;
            }

            _log.Info($"[LC-Publish:success] {sourceLabel} -> {targetEmu}.{targetParam} value={value}");
            return true;
        }

        private static string FormatControlValue(double value)
        {
            return double.IsNaN(value) ? "<init>" : value.ToString("G");
        }

        private static bool TryNormalizeHvBreakerCommand(double rawValue, out double normalizedValue, out bool closed)
        {
            normalizedValue = 0;
            closed = false;
            int cmd = (int)Math.Round(rawValue);
            if (cmd == 0xAA)
            {
                normalizedValue = 0xAA;
                closed = false;
                return true;
            }

            if (cmd == 0xEE)
            {
                normalizedValue = 0xEE;
                closed = true;
                return true;
            }

            return false;
        }

        private static double ToDouble(object raw)
        {
            return raw switch
            {
                bool b => b ? 1 : 0,
                byte v => v,
                sbyte v => v,
                short v => v,
                ushort v => v,
                int v => v,
                uint v => v,
                long v => v,
                ulong v => v,
                float v => v,
                double v => v,
                decimal v => (double)v,
                _ => double.TryParse(raw.ToString(), out var parsed) ? parsed : 0
            };
        }
    }
}

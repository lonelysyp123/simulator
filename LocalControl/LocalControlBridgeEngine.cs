using log4net;

namespace EssSimulator.LocalControl
{
    /// <summary>
    /// LocalControl 报文转发引擎：在 simLc* 与 simEmu* 寄存器之间做聚合与分发，不触碰物理仿真。
    /// </summary>
    internal sealed class LocalControlBridgeEngine
    {
        private readonly ILog _log;
        private readonly Dictionary<string, double> _controlShadow = new();

        /// <summary>EMU 点名（对应 emu.csv）——遥测读取与控制下发共用。</summary>
        private const string EmuPcs1Vab = "yc20";
        private const string EmuPcs1Vbc = "yc21";
        private const string EmuPcs1Vca = "yc22";
        private const string EmuPcs1Freq = "yc23";
        private const string EmuPcs1Status = "yc44";
        private const string EmuPcs1Alarm = "yc45";
        private const string EmuPcs1Fault = "yc46";
        private const string EmuPcs1BlackStart = "yx2";
        private const string EmuPcs1StartStop = "yx3";
        private const string EmuPcs1ActivePower = "yt0";
        private const string EmuPcs1ReactivePower = "yt1";
        private const string EmuPcs1IslandV = "yt3";

        private const string EmuPcs2Vab = "yc47";
        private const string EmuPcs2Vbc = "yc48";
        private const string EmuPcs2Vca = "yc49";
        private const string EmuPcs2Freq = "yc50";
        private const string EmuPcs2Status = "yc71";
        private const string EmuPcs2Alarm = "yc72";
        private const string EmuPcs2Fault = "yc73";
        private const string EmuPcs2BlackStart = "yx4";
        private const string EmuPcs2StartStop = "yx5";
        private const string EmuPcs2ActivePower = "yt4";
        private const string EmuPcs2ReactivePower = "yt5";
        private const string EmuPcs2IslandV = "yt7";

        private const string EmuHvBreaker = "yx0";

        public LocalControlBridgeEngine(ILog log) => _log = log;

        public void RunCycle(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
            int lcIdx,
            int emuPerGroup,
            int emuCount)
        {
            if (!lc.IsOnline)
                return;

            // 含模型绑定的点表（如 trina 系统级点表）由 DataExchange 管道驱动，无需桥接转发
            if (lc.UsesDataExchange)
                return;

            string lcName = lc.ServerName;
            SyncTelemetry(resolveEmu, lc, lcIdx, emuPerGroup, emuCount);
            ApplyControls(resolveEmu, lc, lcName, lcIdx, emuPerGroup, emuCount);
        }

        private void SyncTelemetry(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
            int lcIdx,
            int emuPerGroup,
            int emuCount)
        {
            int startEmu = lcIdx * emuPerGroup;
            bool anyFault = false;
            bool allBlackStart = true;
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

                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                {
                    WritePcsTelemetryDefaults(lc, localPcs);
                    allBlackStart = false;
                    allHvClosed = false;
                    continue;
                }

                bool slot0 = (localPcs % 2) == 0;
                string faultParam = slot0 ? EmuPcs1Fault : EmuPcs2Fault;
                string alarmParam = slot0 ? EmuPcs1Alarm : EmuPcs2Alarm;
                string statusParam = slot0 ? EmuPcs1Status : EmuPcs2Status;
                string freqParam = slot0 ? EmuPcs1Freq : EmuPcs2Freq;
                string vabParam = slot0 ? EmuPcs1Vab : EmuPcs2Vab;
                string vbcParam = slot0 ? EmuPcs1Vbc : EmuPcs2Vbc;
                string vcaParam = slot0 ? EmuPcs1Vca : EmuPcs2Vca;
                string blackStartParam = slot0 ? EmuPcs1BlackStart : EmuPcs2BlackStart;

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
                var hvBreakerVal = ReadParamOrDefault(emu, EmuHvBreaker);

                anyFault |= ModbusValueConverter.ToDouble(faultVal) != 0;
                allBlackStart &= ModbusValueConverter.ToDouble(blackStartVal) != 0;
                allHvClosed &= ModbusValueConverter.ToDouble(hvBreakerVal) != 0;

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
            lc.SetDataStoreByMesurePointName("param3", allHvClosed ? 0xAA : 0xEE);
        }

        private void ApplyControls(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
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
                if (emuIndex >= emuCount)
                    continue;

                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                    continue;

                bool slot0 = (localPcs % 2) == 0;
                string startStopTarget = slot0 ? EmuPcs1StartStop : EmuPcs2StartStop;
                string pTarget = slot0 ? EmuPcs1ActivePower : EmuPcs2ActivePower;
                string qTarget = slot0 ? EmuPcs1ReactivePower : EmuPcs2ReactivePower;
                string islandVTarget = slot0 ? EmuPcs1IslandV : EmuPcs2IslandV;

                int startStopLc = localPcs < 4 ? 60 + localPcs : 80 + (localPcs - 4);
                int pLc = localPcs < 4 ? 64 + localPcs : 84 + (localPcs - 4);
                int qLc = localPcs < 4 ? 68 + localPcs : 88 + (localPcs - 4);
                int islandVLc = localPcs < 4 ? 72 + localPcs : 92 + (localPcs - 4);

                ForwardWhenChanged(lc, lcName, $"param{startStopLc}", emu, startStopTarget, asBool: true);
                ForwardWhenChanged(lc, lcName, $"param{pLc}", emu, pTarget, asBool: false);
                ForwardWhenChanged(lc, lcName, $"param{qLc}", emu, qTarget, asBool: false);
                ForwardWhenChanged(lc, lcName, $"param{islandVLc}", emu, islandVTarget, asBool: false);
            }

            ApplyGlobalBlackStartControl(resolveEmu, lc, lcName, startEmu, emuPerGroup, emuCount);
            ApplyGlobalHvBreakerControl(resolveEmu, lc, lcName, startEmu, emuPerGroup, emuCount);
        }

        private void ApplyGlobalBlackStartControl(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
            string lcName,
            int startEmu,
            int emuPerGroup,
            int emuCount)
        {
            const string lcParam = "param100";
            PrimeGlobalBlackStartShadow(resolveEmu, lc, lcName, startEmu, emuPerGroup, emuCount);
            if (!TryReadChangedControl(lc, lcName, lcParam, asBool: true, out var prevBlackStart, out var blackStartGlobal))
                return;

            _log.Info(
                $"[LC-Change] {lcName}.{lcParam}: {ModbusValueConverter.FormatControlValue(prevBlackStart)} -> {ModbusValueConverter.FormatControlValue(blackStartGlobal)}");

            bool success = true;
            int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
            for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
            {
                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                    continue;

                success &= TryPublishControlWithLog(emu, EmuPcs1BlackStart, blackStartGlobal, asBool: true, $"{lcName}.{lcParam}");
                success &= TryPublishControlWithLog(emu, EmuPcs2BlackStart, blackStartGlobal, asBool: true, $"{lcName}.{lcParam}");
            }

            if (success)
                UpdateControlShadow(lcName, lcParam, blackStartGlobal);
        }

        private void ApplyGlobalHvBreakerControl(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
            string lcName,
            int startEmu,
            int emuPerGroup,
            int emuCount)
        {
            const string lcParam = "param101";
            PrimeGlobalHvBreakerShadow(resolveEmu, lc, lcName, startEmu, emuPerGroup, emuCount);
            if (!TryReadChangedControl(lc, lcName, lcParam, asBool: false, out var prevHvCmd, out var hvRawCmd))
                return;

            if (!ModbusValueConverter.TryNormalizeHvBreakerCommand(hvRawCmd, out var hvCmd, out var hvClosed))
            {
                RevertInvalidHvBreakerCommand(lc, lcName, prevHvCmd);
                return;
            }

            _log.Info(
                $"[LC-Change] {lcName}.{lcParam}: {ModbusValueConverter.FormatControlValue(prevHvCmd)} -> {ModbusValueConverter.FormatControlValue(hvCmd)}");

            bool success = true;
            int endEmu = Math.Min(emuCount, startEmu + emuPerGroup);
            for (int emuIndex = startEmu; emuIndex < endEmu; emuIndex++)
            {
                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                    continue;

                success &= TryPublishControlWithLog(
                    emu,
                    EmuHvBreaker,
                    hvClosed ? 1 : 0,
                    asBool: true,
                    $"{lcName}.{lcParam}");
            }

            if (success)
                UpdateControlShadow(lcName, lcParam, hvCmd);
        }

        private void RevertInvalidHvBreakerCommand(LocalControlModbusServer lc, string lcName, double prevHvCmd)
        {
            if (double.IsNaN(prevHvCmd))
                return;

            try
            {
                lc.SetDataStoreByMesurePointName("param101", prevHvCmd);
            }
            catch (Exception ex)
            {
                _log.Warn($"{lcName}.param101 回退写入失败", ex);
            }
        }

        private static void WritePcsTelemetryDefaults(LocalControlModbusServer lc, int localPcs)
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

        private object ReadParamOrDefault(ModbusSimServer server, string paramName)
        {
            try
            {
                return server.GetDataObjectByMesurePointName(paramName) ?? 0;
            }
            catch (Exception ex)
            {
                _log.Debug($"ReadParamOrDefault 读取 {paramName} 失败，回退 0", ex);
                return 0;
            }
        }

        private void ForwardWhenChanged(
            LocalControlModbusServer lc,
            string lcName,
            string lcParam,
            ModbusSimServer targetEmu,
            string targetParam,
            bool asBool)
        {
            if (!TryReadChangedControl(lc, lcName, lcParam, asBool, out var prevValue, out var currentValue))
                return;

            _log.Info(
                $"[LC-Change] {lcName}.{lcParam}: {ModbusValueConverter.FormatControlValue(prevValue)} -> {ModbusValueConverter.FormatControlValue(currentValue)}");

            bool success;
            if (double.IsNaN(prevValue))
            {
                var targetValue = ModbusValueConverter.ToDouble(ReadParamOrDefault(targetEmu, targetParam));
                targetValue = asBool ? (targetValue != 0 ? 1 : 0) : targetValue;
                success = Math.Abs(targetValue - currentValue) < 1e-9 ||
                          TryPublishControlWithLog(targetEmu, targetParam, currentValue, asBool, $"{lcName}.{lcParam}");
            }
            else
            {
                success = TryPublishControlWithLog(targetEmu, targetParam, currentValue, asBool, $"{lcName}.{lcParam}");
            }

            if (success)
                UpdateControlShadow(lcName, lcParam, currentValue);
        }

        private void PrimeGlobalBlackStartShadow(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
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
                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                {
                    enabled = false;
                    break;
                }

                enabled &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, EmuPcs1BlackStart)) != 0;
                enabled &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, EmuPcs2BlackStart)) != 0;
            }

            double val = enabled ? 1 : 0;
            _controlShadow[key] = val;
            try { lc.SetDataStoreByMesurePointName(lcParam, val); }
            catch { /* 首轮对齐失败可忽略 */ }
        }

        private void PrimeGlobalHvBreakerShadow(
            Func<string, ModbusSimServer?> resolveEmu,
            LocalControlModbusServer lc,
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
                var emu = resolveEmu($"simEmu{emuIndex + 1}");
                if (emu == null || !emu.IsOnline)
                {
                    closed = false;
                    break;
                }

                closed &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, EmuHvBreaker)) != 0;
            }

            double val = closed ? 0xAA : 0xEE;
            _controlShadow[key] = val;
            try { lc.SetDataStoreByMesurePointName(lcParam, val); }
            catch { /* 首轮对齐失败可忽略 */ }
        }

        private bool TryReadChangedControl(
            LocalControlModbusServer lc,
            string lcName,
            string param,
            bool asBool,
            out double previousValue,
            out double currentValue)
        {
            previousValue = 0;
            currentValue = 0;

            object? raw;
            try { raw = lc.GetDataObjectByMesurePointName(param); }
            catch { return false; }

            if (raw == null)
                return false;

            currentValue = ModbusValueConverter.ToDouble(raw);
            currentValue = asBool ? (currentValue != 0 ? 1 : 0) : currentValue;

            string key = $"{lcName}:{param}";
            if (!_controlShadow.TryGetValue(key, out var prev))
            {
                previousValue = double.NaN;
                return true;
            }

            previousValue = prev;
            return Math.Abs(prev - currentValue) >= 1e-9;
        }

        private void UpdateControlShadow(string lcName, string lcParam, double value) =>
            _controlShadow[$"{lcName}:{lcParam}"] = value;

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
                    targetEmu.SetDataObjectByMesurePointName(targetParam, value != 0);
                else
                    targetEmu.SetDataObjectByMesurePointName(targetParam, value);
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
    }
}

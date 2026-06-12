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
            lc.SetDataStoreByMesurePointName("param3", allHvClosed ? 0xEE : 0xAA);
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

                success &= TryPublishControlWithLog(emu, "pcs1_blackstart_enable", blackStartGlobal, asBool: true, $"{lcName}.{lcParam}");
                success &= TryPublishControlWithLog(emu, "pcs2_blackstart_enable", blackStartGlobal, asBool: true, $"{lcName}.{lcParam}");
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
                    "highvoltagebreakeronoff",
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

                enabled &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, "pcs1_blackstart_enable")) != 0;
                enabled &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, "pcs2_blackstart_enable")) != 0;
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

                closed &= ModbusValueConverter.ToDouble(ReadParamOrDefault(emu, "highvoltagebreakeronoff")) != 0;
            }

            double val = closed ? 0xEE : 0xAA;
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
    }
}

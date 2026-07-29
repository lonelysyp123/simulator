using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.Protocol.Modbus;

namespace EssSimulator.DataExchange.Catalog
{
    /// <summary>
    /// 从 Modbus 点表编译数据交互层目录（Phase A：复用 ModbusPointMap 解析结果）。
    /// </summary>
    public static class PointCatalogLoader
    {
        private static readonly Dictionary<string, ControlSemantics> EmuDefaultSemantics = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pcs1_startstop"] = ControlSemantics.Hold,
            ["pcs2_startstop"] = ControlSemantics.Hold,
            ["pcs1_blackstart_enable"] = ControlSemantics.Edge,
            ["pcs2_blackstart_enable"] = ControlSemantics.Edge,
            ["highvoltagebreakeronoff"] = ControlSemantics.Hold
        };

        private static readonly Dictionary<string, ControlSemantics> BmsDefaultSemantics = new(StringComparer.OrdinalIgnoreCase)
        {
            ["param11"] = ControlSemantics.Pulse,
            ["param12"] = ControlSemantics.Hold,
            ["yc133"] = ControlSemantics.Pulse,
            ["yt0"] = ControlSemantics.Pulse
        };

        private static readonly Dictionary<string, ControlEffectId> BmsDefaultEffects = new(StringComparer.OrdinalIgnoreCase)
        {
            ["param11"] = ControlEffectId.BmsApplyLinkCommands,
            ["param12"] = ControlEffectId.BmsApplyLinkCommands,
            ["yc133"] = ControlEffectId.BmsApplyLinkCommands,
            ["yt0"] = ControlEffectId.BmsApplyLinkCommands
        };

        private static readonly Dictionary<string, ControlEffectId> EmuDefaultEffects = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pcs1_startstop"] = ControlEffectId.PcsApplyCommands,
            ["pcs2_startstop"] = ControlEffectId.PcsApplyCommands,
            ["yx3"] = ControlEffectId.PcsApplyCommands,
            ["yx5"] = ControlEffectId.PcsApplyCommands,
            ["pcs1_blackstart_enable"] = ControlEffectId.PcsApplyCommands,
            ["pcs2_blackstart_enable"] = ControlEffectId.PcsApplyCommands,
            ["yx2"] = ControlEffectId.PcsApplyCommands,
            ["yx4"] = ControlEffectId.PcsApplyCommands,
            ["param55"] = ControlEffectId.PcsApplyCommands,
            ["param56"] = ControlEffectId.PcsApplyCommands,
            ["param59"] = ControlEffectId.PcsApplyCommands,
            ["param60"] = ControlEffectId.PcsApplyCommands,
            ["param64"] = ControlEffectId.PcsApplyCommands,
            ["param65"] = ControlEffectId.PcsApplyCommands,
            ["highvoltagebreakeronoff"] = ControlEffectId.UnitHighVoltageBreaker
        };

        public static PointCatalog FromPointMap(
            ModbusPointMap pointMap,
            string serverName,
            DataExchangeOptions? options = null)
        {
            options ??= new DataExchangeOptions();
            bool isEmu = serverName.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase);
            bool isBms = serverName.StartsWith("simBms", StringComparison.OrdinalIgnoreCase);

            var telemetry = new List<PointBinding>();
            foreach (var entry in pointMap.DataMaps)
            {
                if (string.IsNullOrWhiteSpace(entry.ParamName))
                    continue;
                if (!pointMap.ParamModelLookup.TryGetValue(entry.ParamName, out var model))
                    continue;
                if (string.IsNullOrWhiteSpace(model.Arg1))
                    continue;

                var target = DataTarget.ParseBindingPath(model.Arg1);
                if (target == null)
                    continue;

                telemetry.Add(new PointBinding
                {
                    Entry = entry,
                    ParamName = entry.ParamName,
                    Target = target
                });
            }

            var control = new List<PointBinding>();
            foreach (var entry in pointMap.ControlMaps)
            {
                if (string.IsNullOrWhiteSpace(entry.ParamName))
                    continue;
                if (!pointMap.ParamModelLookup.TryGetValue(entry.ParamName, out var model))
                    continue;
                if (string.IsNullOrWhiteSpace(model.Arg1))
                    continue;

                var target = DataTarget.ParseBindingPath(model.Arg1);
                if (target == null)
                    continue;

                control.Add(new PointBinding
                {
                    Entry = entry,
                    ParamName = entry.ParamName,
                    Target = target,
                    Semantics = ResolveSemantics(entry.ParamName, isEmu, isBms, options),
                    Effect = ResolveEffect(entry.ParamName, target, isEmu, isBms, options)
                });
            }

            var rackTelemetry = new List<RackPointBinding>();
            var rackControl = new List<RackPointBinding>();
            if (isBms)
            {
                foreach (var entry in pointMap.RackDataMaps)
                {
                    if (string.IsNullOrWhiteSpace(entry.ParamName))
                        continue;
                    if (!pointMap.RackParamModelLookup.TryGetValue(entry.ParamName, out var model))
                        continue;
                    if (string.IsNullOrWhiteSpace(model.Arg1))
                        continue;

                    rackTelemetry.Add(new RackPointBinding
                    {
                        Entry = entry,
                        ParamName = entry.ParamName,
                        BindingPathTemplate = model.Arg1
                    });
                }

                foreach (var entry in pointMap.RackControlMaps)
                {
                    if (string.IsNullOrWhiteSpace(entry.ParamName))
                        continue;
                    if (!pointMap.RackParamModelLookup.TryGetValue(entry.ParamName, out var model))
                        continue;
                    if (string.IsNullOrWhiteSpace(model.Arg1))
                        continue;

                    rackControl.Add(new RackPointBinding
                    {
                        Entry = entry,
                        ParamName = entry.ParamName,
                        BindingPathTemplate = model.Arg1
                    });
                }
            }

            return new PointCatalog
            {
                ServerName = serverName,
                TelemetryPoints = telemetry,
                ControlPoints = control,
                RackTelemetryPoints = rackTelemetry,
                RackControlPoints = rackControl,
                DefaultValues = new Dictionary<string, object>(pointMap.DefaultBuffer)
            };
        }

        private static ControlSemantics ResolveSemantics(
            string paramName,
            bool isEmu,
            bool isBms,
            DataExchangeOptions options)
        {
            if (options.ControlSemantics.TryGetValue(paramName, out var configured))
                return DataExchangeOptionParser.ParseSemantics(configured, ControlSemantics.Hold);

            if (isEmu && EmuDefaultSemantics.TryGetValue(paramName, out var emuDefault))
                return emuDefault;

            if (isBms && BmsDefaultSemantics.TryGetValue(paramName, out var bmsDefault))
                return bmsDefault;

            return ControlSemantics.Hold;
        }

        private static ControlEffectId ResolveEffect(
            string paramName,
            DataTarget target,
            bool isEmu,
            bool isBms,
            DataExchangeOptions options)
        {
            if (options.ControlEffects.TryGetValue(paramName, out var configured))
                return DataExchangeOptionParser.ParseEffect(configured, ControlEffectId.None);

            if (isEmu && EmuDefaultEffects.TryGetValue(paramName, out var emuDefault))
                return emuDefault;

            if (isBms && BmsDefaultEffects.TryGetValue(paramName, out var bmsDefault))
                return bmsDefault;

            if (isBms && target.PropertyPath.Contains("GridConnectCommand", StringComparison.Ordinal))
                return ControlEffectId.BmsApplyLinkCommands;

            if (isEmu)
            {
                if (target.PropertyPath.Contains("pcsOnOffSwitch", StringComparison.Ordinal) ||
                    target.PropertyPath.Contains("BlackStartEnabled", StringComparison.Ordinal) ||
                    target.PropertyPath.Contains("PCSActivePowerSetting", StringComparison.Ordinal) ||
                    target.PropertyPath.Contains("PCSReactivePowerSetting", StringComparison.Ordinal))
                    return ControlEffectId.PcsApplyCommands;

                if (target.PropertyPath.EndsWith(".PowerOnOff", StringComparison.Ordinal))
                    return ControlEffectId.UnitHighVoltageBreaker;
            }

            return ControlEffectId.None;
        }
    }
}

namespace EssSimulator.Web.Topology
{
    /// <summary>组态连线校验：电压源独占、母线未带电拒绝、变压器匹配、电表 PT/CT 接线等。</summary>
    public static class TopologyValidator
    {
        private const double VoltageMatchTolerancePu = 0.02; // 2%

        public static TopologyValidationResult TryConnect(TopologyProject project, TopologyEdge newEdge)
        {
            if (project == null) return Fail("PROJECT_NULL", "工程为空");
            if (newEdge == null) return Fail("EDGE_NULL", "连线为空");

            newEdge.Id = string.IsNullOrWhiteSpace(newEdge.Id) ? Guid.NewGuid().ToString("N") : newEdge.Id;

            var fromNode = FindNode(project, newEdge.FromNodeId);
            var toNode = FindNode(project, newEdge.ToNodeId);
            if (fromNode == null || toNode == null)
                return Fail("NODE_MISSING", "连线端点设备不存在", newEdge.Id);
            if (fromNode.Id == toNode.Id)
                return Fail("SELF_LINK", "不能将设备连接到自身", newEdge.Id);

            var fromTpl = TopologyTemplates.Get(fromNode.TemplateId);
            var toTpl = TopologyTemplates.Get(toNode.TemplateId);
            if (fromTpl == null || toTpl == null)
                return Fail("TEMPLATE_MISSING", "未知设备模板", newEdge.Id);

            var fromPort = fromTpl.Ports.FirstOrDefault(p => p.Id == newEdge.FromPortId);
            var toPort = toTpl.Ports.FirstOrDefault(p => p.Id == newEdge.ToPortId);
            if (fromPort == null || toPort == null)
                return Fail("PORT_MISSING", "拐角（端口）不存在", newEdge.Id);

            if (IsDuplicateEdge(project, newEdge))
                return Fail("DUP_EDGE", "相同端口之间已存在连接", newEdge.Id);

            // 母线拐角可挂多台设备；普通设备端口仍独占
            if (PortBusyExclusive(project, fromNode, newEdge.FromPortId) ||
                PortBusyExclusive(project, toNode, newEdge.ToPortId))
                return Fail("PORT_BUSY", "端口已被占用，请先断开原有连接", newEdge.Id);

            var domain = CheckDomainCompatibility(fromPort, toPort);
            if (!domain.Ok) { domain.RejectEdgeId = newEdge.Id; return domain; }

            var phase = CheckPhaseCompatibility(fromPort, toPort);
            if (!phase.Ok) { phase.RejectEdgeId = newEdge.Id; return phase; }

            // 变压器自身参数：上大下小
            foreach (var n in new[] { fromNode, toNode })
            {
                if (n.TemplateId == "transformer")
                {
                    double pri = TopologyParamHelper.GetDouble(n.Parameters, "primaryVoltage", 0);
                    double sec = TopologyParamHelper.GetDouble(n.Parameters, "secondaryVoltage", 0);
                    if (pri <= sec)
                        return Fail("XFMR_RATIO", $"变压器「{n.Label}」必须上大下小（一次电压 > 二次电压）", newEdge.Id);
                }
            }

            // AC 母线规则：上侧独占电压源，下侧可挂多台负荷设备
            var busSide = ResolveAcBusSide(fromNode, fromTpl, fromPort, toNode, toTpl, toPort);
            if (busSide != null)
            {
                var busResult = ValidateAcBusConnection(project, busSide.Value.bus, busSide.Value.busTpl,
                    busSide.Value.busPort, busSide.Value.other, busSide.Value.otherTpl, busSide.Value.otherPort, newEdge);
                if (!busResult.Ok) return busResult;
            }

            // 电表：PT/CT 必须接到同一 AC 母线，且相位正确；PT 一次电压匹配
            var meterResult = ValidateMeterConnection(project, fromNode, fromTpl, fromPort, toNode, toTpl, toPort, newEdge);
            if (!meterResult.Ok) return meterResult;

            // 变压器 ↔ 母线电压匹配（母线已带电或已有额定）
            var xfmrResult = ValidateTransformerBusVoltage(fromNode, fromTpl, fromPort, toNode, toTpl, toPort, newEdge);
            if (!xfmrResult.Ok) return xfmrResult;

            // EMU AC 侧电压与母线匹配
            var emuResult = ValidateEmuBusVoltage(fromNode, fromTpl, fromPort, toNode, toTpl, toPort, newEdge);
            if (!emuResult.Ok) return emuResult;

            // DC：单线 dc 只能接到 dc_pos（约定正极）或另一 dc；禁止接到 dc_neg
            var dcResult = ValidateDcPolarity(fromPort, toPort, newEdge);
            if (!dcResult.Ok) return dcResult;

            return new TopologyValidationResult { Ok = true, Message = "连接成功" };
        }

        /// <summary>应用成功连接：写入边，并刷新 AC 母线带电状态。</summary>
        public static TopologyProject ApplyConnect(TopologyProject project, TopologyEdge edge)
        {
            project.Edges.Add(edge);
            RefreshAcBusEnergization(project);
            project.UpdatedAtUtc = DateTime.UtcNow;
            return project;
        }

        public static TopologyProject RemoveEdge(TopologyProject project, string edgeId)
        {
            project.Edges.RemoveAll(e => e.Id == edgeId);
            RefreshAcBusEnergization(project);
            project.UpdatedAtUtc = DateTime.UtcNow;
            return project;
        }

        public static void RefreshAcBusEnergization(TopologyProject project)
        {
            foreach (var bus in project.Nodes.Where(n => n.TemplateId == "ac_bus"))
            {
                var sources = FindVoltageSourcesOnBus(project, bus.Id);
                if (sources.Count == 0)
                {
                    bus.Parameters["energized"] = false;
                    // 保留用户预填 nominalVoltage；若仅由源写入则可清零
                    if (!bus.Parameters.ContainsKey("nominalVoltageLocked") ||
                        !(bus.Parameters["nominalVoltageLocked"] is bool b && b))
                    {
                        // 不断开时若无源，电压记 0 表示未带电
                        bus.Parameters["sourceNodeId"] = null;
                        bus.Parameters["energized"] = false;
                    }
                }
                else
                {
                    var src = sources[0];
                    bus.Parameters["energized"] = true;
                    bus.Parameters["sourceNodeId"] = src.sourceNodeId;
                    bus.Parameters["nominalVoltage"] = src.voltage;
                }
            }
        }

        private static TopologyValidationResult ValidateAcBusConnection(
            TopologyProject project,
            TopologyNode bus,
            TopologyTemplate busTpl,
            TopologyPortDef busPort,
            TopologyNode other,
            TopologyTemplate otherTpl,
            TopologyPortDef otherPort,
            TopologyEdge newEdge)
        {
            // 仅端口级标记计为电压源（避免变压器一次侧被误判为第二电源）
            bool otherIsSource = otherPort.IsVoltageSourcePort;
            bool busTop = string.Equals(busPort.Side, "top", StringComparison.OrdinalIgnoreCase);

            var existingSources = FindVoltageSourcesOnBus(project, bus.Id);

            // 上侧：只接受电压源，且每相拐角独占（占用检查在 PortBusyExclusive）
            if (busTop)
            {
                if (!otherIsSource)
                {
                    return Fail(
                        "BUS_TOP_SOURCE_ONLY",
                        $"母线「{bus.Label}」上侧拐角只能连接电压源（电网，或变压器二次侧），不能接入「{other.Label}」。负荷设备请接到下侧拐角。",
                        newEdge.Id);
                }

                if (existingSources.Count > 0 &&
                    existingSources.All(s => s.sourceNodeId != other.Id))
                {
                    return Fail(
                        "BUS_MULTI_SOURCE",
                        $"母线「{bus.Label}」已接入电压源「{LabelOf(project, existingSources[0].sourceNodeId)}」，" +
                        $"禁止再接入「{other.Label}」。已拒绝本次连接。",
                        newEdge.Id,
                        new List<string>
                        {
                            $"既有电压源节点: {existingSources[0].sourceNodeId}",
                            $"冲突电压源节点: {other.Id}"
                        });
                }

                double v = PortVoltage(other, otherPort);
                double busV = TopologyParamHelper.GetDouble(bus.Parameters, "nominalVoltage", 0);
                bool energized = IsTruthy(bus.Parameters, "energized") || existingSources.Count > 0;
                if (energized && busV > 1 && !VoltageMatches(busV, v))
                {
                    return Fail(
                        "BUS_VOLTAGE_MISMATCH",
                        $"电压源「{other.Label}」输出 {v:0.##} V，与母线「{bus.Label}」电压 {busV:0.##} V 不匹配",
                        newEdge.Id);
                }

                return Ok();
            }

            // 下侧：挂载负荷/测量设备，允许多台；电压源应接上侧
            if (otherIsSource)
            {
                return Fail(
                    "BUS_BOTTOM_LOAD_ONLY",
                    $"母线「{bus.Label}」下侧拐角用于挂载设备，电压源「{other.Label}」请接到上侧拐角。",
                    newEdge.Id);
            }

            if (existingSources.Count == 0)
            {
                return Fail(
                    "BUS_NO_SOURCE",
                    $"母线「{bus.Label}」尚未接入电压源，拒绝接入设备「{other.Label}」。请先在上侧连接电网或变压器二次侧。",
                    newEdge.Id);
            }

            return Ok();
        }

        private static TopologyValidationResult ValidateMeterConnection(
            TopologyProject project,
            TopologyNode a, TopologyTemplate aTpl, TopologyPortDef aPort,
            TopologyNode b, TopologyTemplate bTpl, TopologyPortDef bPort,
            TopologyEdge newEdge)
        {
            TopologyNode? meter = null;
            TopologyPortDef? meterPort = null;
            TopologyNode? other = null;
            TopologyPortDef? otherPort = null;

            if (a.TemplateId == "ac_meter") { meter = a; meterPort = aPort; other = b; otherPort = bPort; }
            else if (b.TemplateId == "ac_meter") { meter = b; meterPort = bPort; other = a; otherPort = aPort; }
            else return Ok();

            if (other!.TemplateId != "ac_bus")
                return Fail("METER_NOT_BUS", $"电表「{meter!.Label}」的 {meterPort!.Label} 必须连接到三相母线，不能接到「{other.Label}」", newEdge.Id);

            if (otherPort!.Phase != meterPort!.Phase)
                return Fail("METER_PHASE", $"电表端口 {meterPort.Label}（{meterPort.Phase} 相）与母线端口 {otherPort.Label}（{otherPort.Phase} 相）相位不一致", newEdge.Id);

            // PT 端口：一次电压匹配母线
            if (meterPort.Id.StartsWith("pt_", StringComparison.Ordinal))
            {
                double ptV = TopologyParamHelper.GetDouble(meter.Parameters, "ptPrimaryVoltage", 0);
                double busV = ResolveBusVoltage(project, other);
                if (busV > 1 && !VoltageMatches(busV, ptV))
                    return Fail("METER_PT_MISMATCH",
                        $"电表 PT 一次 {ptV:0.##} V 与母线「{other.Label}」电压 {busV:0.##} V 不匹配", newEdge.Id);
            }

            // CT 与 PT 须在同一母线（检查已有连接）
            var meterEdges = project.Edges.Where(e => e.FromNodeId == meter.Id || e.ToNodeId == meter.Id).ToList();
            foreach (var e in meterEdges)
            {
                var otherId = e.FromNodeId == meter.Id ? e.ToNodeId : e.FromNodeId;
                if (otherId != other.Id && project.Nodes.Any(n => n.Id == otherId && n.TemplateId == "ac_bus"))
                    return Fail("METER_MULTI_BUS", $"电表「{meter.Label}」的 PT/CT 必须全部接到同一母线", newEdge.Id);
            }

            return Ok();
        }

        private static TopologyValidationResult ValidateTransformerBusVoltage(
            TopologyNode a, TopologyTemplate aTpl, TopologyPortDef aPort,
            TopologyNode b, TopologyTemplate bTpl, TopologyPortDef bPort,
            TopologyEdge newEdge)
        {
            TopologyNode? xfmr = null; TopologyPortDef? xfmrPort = null;
            TopologyNode? bus = null;

            if (a.TemplateId == "transformer" && b.TemplateId == "ac_bus")
            { xfmr = a; xfmrPort = aPort; bus = b; }
            else if (b.TemplateId == "transformer" && a.TemplateId == "ac_bus")
            { xfmr = b; xfmrPort = bPort; bus = a; }
            else return Ok();

            double termV = PortVoltage(xfmr!, xfmrPort!);
            double busV = TopologyParamHelper.GetDouble(bus!.Parameters, "nominalVoltage", 0);
            bool energized = IsTruthy(bus.Parameters, "energized");

            if (energized && busV > 1 && !VoltageMatches(busV, termV))
            {
                return Fail(
                    "XFMR_BUS_MISMATCH",
                    $"变压器「{xfmr!.Label}」{xfmrPort!.Label} 侧 {termV:0.##} V 与母线「{bus.Label}」{busV:0.##} V 不匹配，已拒绝连接",
                    newEdge.Id);
            }

            // 母线未带电但用户预填了额定电压
            if (!energized && busV > 1 && !VoltageMatches(busV, termV))
            {
                return Fail(
                    "XFMR_BUS_MISMATCH",
                    $"变压器「{xfmr!.Label}」{xfmrPort!.Label} 侧 {termV:0.##} V 与母线预设 {busV:0.##} V 不匹配",
                    newEdge.Id);
            }

            return Ok();
        }

        private static TopologyValidationResult ValidateEmuBusVoltage(
            TopologyNode a, TopologyTemplate aTpl, TopologyPortDef aPort,
            TopologyNode b, TopologyTemplate bTpl, TopologyPortDef bPort,
            TopologyEdge newEdge)
        {
            TopologyNode? emu = null; TopologyPortDef? emuPort = null; TopologyNode? bus = null;
            if (a.TemplateId == "emu" && b.TemplateId == "ac_bus")
            { emu = a; emuPort = aPort; bus = b; }
            else if (b.TemplateId == "emu" && a.TemplateId == "ac_bus")
            { emu = b; emuPort = bPort; bus = a; }
            else return Ok();

            if (!emuPort!.Kind.StartsWith("ac")) return Ok();

            double emuV = TopologyParamHelper.GetDouble(emu!.Parameters, "acVoltage", 0);
            double busV = TopologyParamHelper.GetDouble(bus!.Parameters, "nominalVoltage", 0);
            if (busV > 1 && !VoltageMatches(busV, emuV))
                return Fail("EMU_BUS_MISMATCH",
                    $"EMU「{emu.Label}」交流侧 {emuV:0.##} V 与母线「{bus.Label}」{busV:0.##} V 不匹配", newEdge.Id);

            return Ok();
        }

        private static TopologyValidationResult ValidateDcPolarity(
            TopologyPortDef a, TopologyPortDef b, TopologyEdge newEdge)
        {
            bool aDc = IsDc(a.Kind);
            bool bDc = IsDc(b.Kind);
            if (!aDc || !bDc) return Ok();

            // 统一为正/负双拐角：同极性相接，禁止正负对短
            string pa = NormalizeDcPolarity(a.Kind);
            string pb = NormalizeDcPolarity(b.Kind);
            if (pa != pb)
            {
                return Fail(
                    "DC_POLARITY",
                    $"直流极性不匹配：{a.Label}({PolarityLabel(pa)}) ↔ {b.Label}({PolarityLabel(pb)})。正极只能接正极，负极只能接负极。",
                    newEdge.Id);
            }

            return Ok();
        }

        private static string NormalizeDcPolarity(string kind) => kind switch
        {
            "dc_pos" => "pos",
            "dc_neg" => "neg",
            // 兼容旧工程里残留的无极性 dc，按正极处理
            "dc" => "pos",
            _ => kind
        };

        private static string PolarityLabel(string p) => p == "neg" ? "负极" : "正极";

        private static TopologyValidationResult CheckDomainCompatibility(TopologyPortDef a, TopologyPortDef b)
        {
            bool aAc = a.Kind == "ac_phase";
            bool bAc = b.Kind == "ac_phase";
            bool aDc = IsDc(a.Kind);
            bool bDc = IsDc(b.Kind);

            if (aAc && bAc) return Ok();
            if (aDc && bDc) return Ok();
            return Fail("DOMAIN_MISMATCH", $"端口类型不兼容：{a.Label}({a.Kind}) ↔ {b.Label}({b.Kind})。交流不能接直流。");
        }

        private static TopologyValidationResult CheckPhaseCompatibility(TopologyPortDef a, TopologyPortDef b)
        {
            if (a.Kind != "ac_phase" || b.Kind != "ac_phase") return Ok();
            if (string.IsNullOrEmpty(a.Phase) || string.IsNullOrEmpty(b.Phase)) return Ok();
            if (a.Phase == b.Phase) return Ok();
            return Fail("PHASE_MISMATCH", $"相位不匹配：{a.Label}({a.Phase}) ↔ {b.Label}({b.Phase})");
        }

        private static (TopologyNode bus, TopologyTemplate busTpl, TopologyPortDef busPort, TopologyNode other, TopologyTemplate otherTpl, TopologyPortDef otherPort)?
            ResolveAcBusSide(
                TopologyNode a, TopologyTemplate aTpl, TopologyPortDef aPort,
                TopologyNode b, TopologyTemplate bTpl, TopologyPortDef bPort)
        {
            if (a.TemplateId == "ac_bus" && aPort.Kind == "ac_phase")
                return (a, aTpl, aPort, b, bTpl, bPort);
            if (b.TemplateId == "ac_bus" && bPort.Kind == "ac_phase")
                return (b, bTpl, bPort, a, aTpl, aPort);
            return null;
        }

        private static List<(string sourceNodeId, double voltage)> FindVoltageSourcesOnBus(
            TopologyProject project, string busId)
        {
            var list = new List<(string, double)>();
            foreach (var e in project.Edges)
            {
                string? otherId = null;
                string? otherPortId = null;
                if (e.FromNodeId == busId) { otherId = e.ToNodeId; otherPortId = e.ToPortId; }
                else if (e.ToNodeId == busId) { otherId = e.FromNodeId; otherPortId = e.FromPortId; }
                else continue;

                var other = FindNode(project, otherId);
                if (other == null) continue;
                var tpl = TopologyTemplates.Get(other.TemplateId);
                var port = tpl?.Ports.FirstOrDefault(p => p.Id == otherPortId);
                if (port == null) continue;
                if (!port.IsVoltageSourcePort) continue;

                list.Add((other.Id, PortVoltage(other, port)));
            }

            return list
                .GroupBy(x => x.Item1)
                .Select(g => g.First())
                .ToList();
        }

        private static double ResolveBusVoltage(TopologyProject project, TopologyNode bus)
        {
            var sources = FindVoltageSourcesOnBus(project, bus.Id);
            if (sources.Count > 0) return sources[0].voltage;
            return TopologyParamHelper.GetDouble(bus.Parameters, "nominalVoltage", 0);
        }

        private static double PortVoltage(TopologyNode node, TopologyPortDef port)
        {
            if (!string.IsNullOrEmpty(port.VoltageParam))
                return TopologyParamHelper.GetDouble(node.Parameters, port.VoltageParam!, 0);
            return 0;
        }

        private static bool VoltageMatches(double a, double b)
        {
            if (a <= 0 || b <= 0) return false;
            double mid = (a + b) / 2.0;
            return Math.Abs(a - b) / mid <= VoltageMatchTolerancePu;
        }

        /// <summary>
        /// 直流母线全部端口、三相母线下侧端口允许一对多；
        /// 三相母线上侧与普通设备端口独占。
        /// </summary>
        private static bool AllowsMultiConnect(TopologyNode node, string portId)
        {
            if (node.TemplateId == "dc_bus")
                return true;
            if (node.TemplateId == "ac_bus")
            {
                var port = TopologyTemplates.Get(node.TemplateId)?.Ports.FirstOrDefault(p => p.Id == portId);
                return string.Equals(port?.Side, "bottom", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static bool PortBusyExclusive(TopologyProject project, TopologyNode node, string portId)
        {
            if (AllowsMultiConnect(node, portId))
                return false;
            return project.Edges.Any(e =>
                (e.FromNodeId == node.Id && e.FromPortId == portId) ||
                (e.ToNodeId == node.Id && e.ToPortId == portId));
        }

        private static bool IsDuplicateEdge(TopologyProject project, TopologyEdge edge) =>
            project.Edges.Any(e =>
                (e.FromNodeId == edge.FromNodeId && e.FromPortId == edge.FromPortId &&
                 e.ToNodeId == edge.ToNodeId && e.ToPortId == edge.ToPortId) ||
                (e.FromNodeId == edge.ToNodeId && e.FromPortId == edge.ToPortId &&
                 e.ToNodeId == edge.FromNodeId && e.ToPortId == edge.FromPortId));

        private static TopologyNode? FindNode(TopologyProject p, string id) =>
            p.Nodes.FirstOrDefault(n => n.Id == id);

        private static string LabelOf(TopologyProject p, string id) =>
            FindNode(p, id)?.Label ?? id;

        private static bool IsDc(string kind) =>
            kind is "dc" or "dc_pos" or "dc_neg";

        private static bool IsTruthy(Dictionary<string, object?> parameters, string key)
        {
            if (!parameters.TryGetValue(key, out var raw) || raw == null) return false;
            if (raw is bool b) return b;
            if (raw is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                if (je.ValueKind == System.Text.Json.JsonValueKind.False) return false;
            }
            return string.Equals(raw.ToString(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static TopologyValidationResult Ok() => new() { Ok = true, Message = "ok" };

        private static TopologyValidationResult Fail(string code, string message, string? rejectEdgeId = null, List<string>? details = null) =>
            new()
            {
                Ok = false,
                Code = code,
                Message = message,
                RejectEdgeId = rejectEdgeId,
                Details = details ?? new List<string>()
            };
    }
}

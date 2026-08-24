namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 组态校验：编辑连线只做结构完整性；电气连接规则在保存时从上到下回放，遇到第一处问题即中断。
    /// </summary>
    public static class TopologyValidator
    {
        private const double VoltageMatchTolerancePu = 0.02; // 2%

        /// <summary>编辑连线：只检查端点/端口存在、禁止自连与重复边，不套用电气连接规则。</summary>
        public static TopologyValidationResult TryConnect(TopologyProject project, TopologyEdge newEdge) =>
            TryAttachEdge(project, newEdge, enforceElectricalRules: false);

        /// <summary>保存回放用：结构 + 电气连接规则（相位、母线上下侧、电压匹配等）。</summary>
        public static TopologyValidationResult ValidateConnectionRules(TopologyProject project, TopologyEdge newEdge) =>
            TryAttachEdge(project, newEdge, enforceElectricalRules: true);

        private static TopologyValidationResult TryAttachEdge(
            TopologyProject project, TopologyEdge newEdge, bool enforceElectricalRules)
        {
            if (project == null) return Fail("PROJECT_NULL", "工程为空");
            if (newEdge == null) return Fail("EDGE_NULL", "连线为空");

            newEdge.Id = string.IsNullOrWhiteSpace(newEdge.Id) ? Guid.NewGuid().ToString("N") : newEdge.Id;

            var fromNode = FindNode(project, newEdge.FromNodeId);
            var toNode = FindNode(project, newEdge.ToNodeId);
            if (fromNode == null || toNode == null)
            {
                var missing = fromNode == null ? newEdge.FromNodeId : newEdge.ToNodeId;
                return Fail("NODE_MISSING", $"连线端点设备不存在（节点 {missing}），请删除指向它的残留连线", newEdge.Id);
            }
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

            if (!enforceElectricalRules)
                return new TopologyValidationResult { Ok = true, Message = "连接成功" };

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

            // 电表：统一 PT/CT 三相口须接同一 AC 母线，相位正确，且 PT 一次电压匹配
            var meterResult = ValidateMeterConnection(project, fromNode, fromTpl, fromPort, toNode, toTpl, toPort, newEdge);
            if (!meterResult.Ok) return meterResult;

            // 变压器 ↔ 母线电压匹配（母线已带电或已有额定）
            var xfmrResult = ValidateTransformerBusVoltage(fromNode, fromTpl, fromPort, toNode, toTpl, toPort, newEdge);
            if (!xfmrResult.Ok) return xfmrResult;

            // PCS / 光伏单元 AC 侧电压与母线匹配
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
            var buses = project.Nodes.Where(n => n.TemplateId == "ac_bus").ToList();
            foreach (var bus in buses)
            {
                bus.Parameters["energized"] = false;
                bus.Parameters["sourceNodeId"] = null;
            }

            bool changed;
            var guard = 0;
            do
            {
                changed = false;
                foreach (var bus in buses)
                {
                    var sources = FindVoltageSourcesOnBus(project, bus.Id);
                    if (sources.Count == 0)
                        continue;

                    var src = sources[0];
                    bool was = IsTruthy(bus.Parameters, "energized");
                    double oldV = TopologyParamHelper.GetDouble(bus.Parameters, "nominalVoltage", 0);
                    string? oldSrc = bus.Parameters.TryGetValue("sourceNodeId", out var raw)
                        ? raw?.ToString()
                        : null;
                    if (!was || Math.Abs(oldV - src.voltage) > 0.01 || oldSrc != src.sourceNodeId)
                    {
                        bus.Parameters["energized"] = true;
                        bus.Parameters["sourceNodeId"] = src.sourceNodeId;
                        bus.Parameters["nominalVoltage"] = src.voltage;
                        changed = true;
                    }
                }
            } while (changed && ++guard < 32);
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
            bool otherIsBreaker = other.TemplateId == "ac_breaker";
            bool busTop = string.Equals(busPort.Side, "top", StringComparison.OrdinalIgnoreCase);

            var existingSources = FindVoltageSourcesOnBus(project, bus.Id);

            // 上侧：电压源，或串联三相断路器（其后可接电压源）
            if (busTop)
            {
                if (!otherIsSource && !otherIsBreaker)
                {
                    return Fail(
                        "BUS_TOP_SOURCE_ONLY",
                        $"母线「{bus.Label}」上侧拐角只能连接电压源（电网，或变压器二次侧）或三相断路器，不能接入「{other.Label}」。负荷设备请接到下侧拐角。",
                        newEdge.Id);
                }

                // 断路器对侧已有电压源时，按该源做多源/电压校验
                var incomingSources = otherIsBreaker
                    ? FindVoltageSourcesBehindBreaker(project, other, otherPort.Id)
                    : new List<(string sourceNodeId, double voltage)> { (other.Id, PortVoltage(other, otherPort)) };

                foreach (var src in incomingSources)
                {
                    if (existingSources.Count > 0 &&
                        existingSources.All(s => s.sourceNodeId != src.sourceNodeId))
                    {
                        return Fail(
                            "BUS_MULTI_SOURCE",
                            $"母线「{bus.Label}」已接入电压源「{LabelOf(project, existingSources[0].sourceNodeId)}」，" +
                            $"禁止再接入「{LabelOf(project, src.sourceNodeId)}」。已拒绝本次连接。",
                            newEdge.Id,
                            new List<string>
                            {
                                $"既有电压源节点: {existingSources[0].sourceNodeId}",
                                $"冲突电压源节点: {src.sourceNodeId}"
                            });
                    }

                    double busV = TopologyParamHelper.GetDouble(bus.Parameters, "nominalVoltage", 0);
                    bool energized = IsTruthy(bus.Parameters, "energized") || existingSources.Count > 0;
                    if (energized && busV > 1 && src.voltage > 1 && !VoltageMatches(busV, src.voltage))
                    {
                        return Fail(
                            "BUS_VOLTAGE_MISMATCH",
                            $"电压源「{LabelOf(project, src.sourceNodeId)}」输出 {src.voltage:0.##} V，与母线「{bus.Label}」电压 {busV:0.##} V 不匹配",
                            newEdge.Id);
                    }
                }

                return Ok();
            }

            // 下侧：挂载负荷/测量设备/串联断路器，允许多台；电压源应接上侧
            if (otherIsSource)
            {
                return Fail(
                    "BUS_BOTTOM_LOAD_ONLY",
                    $"母线「{bus.Label}」下侧拐角用于挂载设备，电压源「{other.Label}」请接到上侧拐角。",
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
                return Fail("METER_NOT_BUS",
                    $"电表「{meter!.Label}」的 {meterPort!.Label}（PT/CT 统一口）必须连接到三相母线，不能接到「{other.Label}」",
                    newEdge.Id);

            if (otherPort!.Phase != meterPort!.Phase)
                return Fail("METER_PHASE",
                    $"电表端口 {meterPort.Label}（{meterPort.Phase} 相）与母线端口 {otherPort.Label}（{otherPort.Phase} 相）相位不一致",
                    newEdge.Id);

            // 统一抽头：该口同时承载 PT/CT，校验 PT 一次与母线电压
            double ptV = TopologyParamHelper.GetDouble(meter.Parameters, "ptPrimaryVoltage", 0);
            double busV = ResolveBusVoltage(project, other);
            if (busV > 1 && !VoltageMatches(busV, ptV))
                return Fail("METER_PT_MISMATCH",
                    $"电表 PT 一次 {ptV:0.##} V 与母线「{other.Label}」电压 {busV:0.##} V 不匹配（PT/CT 共用端口）",
                    newEdge.Id);

            // 三相统一口须接到同一母线
            var meterEdges = project.Edges.Where(e => e.FromNodeId == meter.Id || e.ToNodeId == meter.Id).ToList();
            foreach (var e in meterEdges)
            {
                var otherId = e.FromNodeId == meter.Id ? e.ToNodeId : e.FromNodeId;
                if (otherId != other.Id && project.Nodes.Any(n => n.Id == otherId && n.TemplateId == "ac_bus"))
                    return Fail("METER_MULTI_BUS", $"电表「{meter.Label}」的 PT/CT 三相口必须全部接到同一母线", newEdge.Id);
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

        private static bool IsAcFeederUnit(string templateId) =>
            templateId == "pcs" || templateId == "pv_unit";

        private static TopologyValidationResult ValidateEmuBusVoltage(
            TopologyNode a, TopologyTemplate aTpl, TopologyPortDef aPort,
            TopologyNode b, TopologyTemplate bTpl, TopologyPortDef bPort,
            TopologyEdge newEdge)
        {
            TopologyNode? unit = null; TopologyPortDef? unitPort = null; TopologyNode? bus = null;
            if (IsAcFeederUnit(a.TemplateId) && b.TemplateId == "ac_bus")
            { unit = a; unitPort = aPort; bus = b; }
            else if (IsAcFeederUnit(b.TemplateId) && a.TemplateId == "ac_bus")
            { unit = b; unitPort = bPort; bus = a; }
            else return Ok();

            if (!unitPort!.Kind.StartsWith("ac")) return Ok();

            double unitV = TopologyParamHelper.GetDouble(unit!.Parameters, "acVoltage", 0);
            double busV = TopologyParamHelper.GetDouble(bus!.Parameters, "nominalVoltage", 0);
            if (busV > 1 && !VoltageMatches(busV, unitV))
            {
                bool pv = unit.TemplateId == "pv_unit";
                return Fail(
                    pv ? "PV_BUS_MISMATCH" : "PCS_BUS_MISMATCH",
                    $"{(pv ? "光伏单元" : "PCS")}「{unit.Label}」交流侧 {unitV:0.##} V 与母线「{bus.Label}」{busV:0.##} V 不匹配",
                    newEdge.Id);
            }

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
            var visited = new HashSet<string>(StringComparer.Ordinal) { busId };
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

                if (other.TemplateId == "ac_breaker")
                {
                    list.AddRange(CollectLiveThroughBreaker(project, other, otherPortId!, visited));
                    continue;
                }

                if (other.TemplateId == "ac_bus")
                {
                    list.AddRange(LiveFromEnergizedBus(other));
                    continue;
                }

                if (!port.IsVoltageSourcePort) continue;
                list.Add((other.Id, PortVoltage(other, port)));
            }

            return list
                .GroupBy(x => x.Item1)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>经断路器对侧查找带电上一级（合闸时）：电压源、已带电母线，或再经合闸断路器穿越。</summary>
        private static List<(string sourceNodeId, double voltage)> FindVoltageSourcesBehindBreaker(
            TopologyProject project, TopologyNode breaker, string portFacingBus) =>
            CollectLiveThroughBreaker(project, breaker, portFacingBus, new HashSet<string>(StringComparer.Ordinal));

        private static List<(string sourceNodeId, double voltage)> CollectLiveThroughBreaker(
            TopologyProject project, TopologyNode breaker, string portFacingBus, HashSet<string> visited)
        {
            var list = new List<(string, double)>();
            if (!IsBreakerClosed(breaker)) return list;
            if (!visited.Add(breaker.Id)) return list;

            string? opposite = OppositeBreakerPort(portFacingBus);
            if (opposite == null) return list;

            foreach (var e in project.Edges)
            {
                string? otherId = null;
                string? otherPortId = null;
                if (e.FromNodeId == breaker.Id && e.FromPortId == opposite)
                { otherId = e.ToNodeId; otherPortId = e.ToPortId; }
                else if (e.ToNodeId == breaker.Id && e.ToPortId == opposite)
                { otherId = e.FromNodeId; otherPortId = e.FromPortId; }
                else continue;

                var other = FindNode(project, otherId);
                if (other == null || otherPortId == null) continue;
                list.AddRange(CollectLiveAt(project, other, otherPortId, visited));
            }

            return list
                .GroupBy(x => x.Item1)
                .Select(g => g.First())
                .ToList();
        }

        private static List<(string sourceNodeId, double voltage)> CollectLiveAt(
            TopologyProject project, TopologyNode node, string portId, HashSet<string> visited)
        {
            if (node.TemplateId == "ac_breaker")
                return CollectLiveThroughBreaker(project, node, portId, visited);

            if (node.TemplateId == "ac_bus")
                return LiveFromEnergizedBus(node);

            var tpl = TopologyTemplates.Get(node.TemplateId);
            var port = tpl?.Ports.FirstOrDefault(p => p.Id == portId);
            if (port == null || !port.IsVoltageSourcePort)
                return new List<(string, double)>();
            return new List<(string, double)> { (node.Id, PortVoltage(node, port)) };
        }

        private static List<(string sourceNodeId, double voltage)> LiveFromEnergizedBus(TopologyNode bus)
        {
            var list = new List<(string, double)>();
            if (!IsTruthy(bus.Parameters, "energized")) return list;
            double v = TopologyParamHelper.GetDouble(bus.Parameters, "nominalVoltage", 0);
            if (v > 1)
                list.Add((BusSourceId(bus), v));
            return list;
        }

        private static string BusSourceId(TopologyNode bus)
        {
            if (bus.Parameters.TryGetValue("sourceNodeId", out var raw) && raw != null)
            {
                var s = raw.ToString();
                if (!string.IsNullOrWhiteSpace(s)) return s;
            }
            return bus.Id;
        }

        private static string? OppositeBreakerPort(string portId) => portId switch
        {
            "a" => "a2",
            "b" => "b2",
            "c" => "c2",
            "a2" => "a",
            "b2" => "b",
            "c2" => "c",
            _ => null
        };

        private static bool IsBreakerClosed(TopologyNode breaker)
        {
            if (!breaker.Parameters.TryGetValue("closed", out var v) || v == null) return true;
            if (v is bool b) return b;
            if (v is System.Text.Json.JsonElement je)
            {
                if (je.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                if (je.ValueKind == System.Text.Json.JsonValueKind.False) return false;
                if (je.ValueKind == System.Text.Json.JsonValueKind.String &&
                    bool.TryParse(je.GetString(), out var pb)) return pb;
            }
            if (bool.TryParse(v.ToString(), out var parsed)) return parsed;
            return true;
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

        /// <summary>
        /// 将单条连线扩展为同组端口：交流同侧 A/B/C，或直流同侧正/负。
        /// 非成组端口则原样返回一条。
        /// </summary>
        public static List<TopologyEdge> ExpandBundle(TopologyProject project, TopologyEdge seed)
        {
            if (project == null || seed == null) return new List<TopologyEdge>();

            var fromNode = FindNode(project, seed.FromNodeId);
            var toNode = FindNode(project, seed.ToNodeId);
            if (fromNode == null || toNode == null)
                return new List<TopologyEdge> { CloneEdge(seed) };

            var fromTpl = TopologyTemplates.Get(fromNode.TemplateId);
            var toTpl = TopologyTemplates.Get(toNode.TemplateId);
            if (fromTpl == null || toTpl == null)
                return new List<TopologyEdge> { CloneEdge(seed) };

            var fromPort = fromTpl.Ports.FirstOrDefault(p => p.Id == seed.FromPortId);
            var toPort = toTpl.Ports.FirstOrDefault(p => p.Id == seed.ToPortId);
            if (fromPort == null || toPort == null)
                return new List<TopologyEdge> { CloneEdge(seed) };

            // 交流三相：按相位配对，限定在各自所选侧
            if (fromPort.Kind == "ac_phase" && toPort.Kind == "ac_phase" &&
                !string.IsNullOrEmpty(fromPort.Phase) && !string.IsNullOrEmpty(toPort.Phase))
            {
                var fromGroup = fromTpl.Ports
                    .Where(p => p.Kind == "ac_phase" &&
                                string.Equals(p.Side, fromPort.Side, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrEmpty(p.Phase))
                    .ToList();
                var toByPhase = toTpl.Ports
                    .Where(p => p.Kind == "ac_phase" &&
                                string.Equals(p.Side, toPort.Side, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrEmpty(p.Phase))
                    .GroupBy(p => p.Phase!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                if (fromGroup.Count >= 2 && toByPhase.Count >= 2)
                {
                    var list = new List<TopologyEdge>();
                    foreach (var fp in fromGroup.OrderBy(p => PhaseOrder(p.Phase)))
                    {
                        if (!toByPhase.TryGetValue(fp.Phase!, out var tp)) continue;
                        list.Add(new TopologyEdge
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            FromNodeId = seed.FromNodeId,
                            FromPortId = fp.Id,
                            ToNodeId = seed.ToNodeId,
                            ToPortId = tp.Id
                        });
                    }
                    if (list.Count > 0) return list;
                }
            }

            // 直流正负：同侧成对
            if (IsDc(fromPort.Kind) && IsDc(toPort.Kind))
            {
                var fromPair = FindDcPair(fromTpl, fromPort);
                var toPair = FindDcPair(toTpl, toPort);
                if (fromPair != null && toPair != null)
                {
                    return new List<TopologyEdge>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            FromNodeId = seed.FromNodeId,
                            FromPortId = fromPair.Value.pos.Id,
                            ToNodeId = seed.ToNodeId,
                            ToPortId = toPair.Value.pos.Id
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            FromNodeId = seed.FromNodeId,
                            FromPortId = fromPair.Value.neg.Id,
                            ToNodeId = seed.ToNodeId,
                            ToPortId = toPair.Value.neg.Id
                        }
                    };
                }
            }

            return new List<TopologyEdge> { CloneEdge(seed) };
        }

        /// <summary>
        /// 成组连接（编辑）：先 ExpandBundle，再逐条写入；不做电气规则校验。
        /// 任一条结构失败则整组回滚（不修改入参 project）。
        /// </summary>
        public static TopologyValidationResult TryConnectBundle(
            TopologyProject project,
            TopologyEdge seed,
            out TopologyProject? updated)
        {
            updated = null;
            if (project == null || seed == null)
                return Fail("BAD_REQUEST", "工程或连线为空");

            var bundle = ExpandBundle(project, seed);
            if (bundle.Count == 0)
                return Fail("BUNDLE_EMPTY", "无法生成连线");

            var work = CloneProject(project);
            var applied = 0;
            foreach (var edge in bundle)
            {
                if (IsDuplicateEdge(work, edge))
                    continue;

                var validation = TryConnect(work, edge);
                if (!validation.Ok)
                {
                    validation.ProblemNodeIds = new List<string> { seed.FromNodeId, seed.ToNodeId }
                        .Distinct(StringComparer.Ordinal)
                        .ToList();
                    return validation;
                }

                ApplyConnect(work, edge);
                applied++;
            }

            updated = work;
            return new TopologyValidationResult
            {
                Ok = true,
                Message = applied == 0 ? "连接已存在" : $"已连接 {applied} 条",
                Details = { $"成组端口 {bundle.Count}，新建 {applied}" }
            };
        }

        /// <summary>
        /// 保存工程：先查电网/主断/并网点电表，再按画布从上到下回放连线套用电气规则，遇到第一处问题即中断。
        /// </summary>
        public static TopologyValidationResult ValidateProjectForSave(TopologyProject project)
        {
            if (project == null)
                return Fail("PROJECT_NULL", "工程为空");

            var details = new List<string>();
            var grids = project.Nodes.Where(n => n.TemplateId == "grid").ToList();
            if (grids.Count == 0)
            {
                details.Add("缺少电网模型");
                return Fail("NEED_GRID", "工程须包含至少一个电网模型",
                    details: details,
                    problemNodeIds: project.Nodes.Select(n => n.Id).ToList());
            }

            var mainBreakers = project.Nodes
                .Where(n => n.TemplateId == "ac_breaker" && TopologyParamHelper.GetBool(n.Parameters, "isMainBreaker"))
                .ToList();
            if (mainBreakers.Count == 0)
            {
                var breakers = project.Nodes.Where(n => n.TemplateId == "ac_breaker").Select(n => n.Id).ToList();
                details.Add("未指定主断路器（请在三相断路器属性中勾选「作为主断路器」）");
                return Fail("NEED_MAIN_BREAKER", "须指定有且仅有一个主断路器",
                    details: details, problemNodeIds: breakers);
            }
            if (mainBreakers.Count > 1)
            {
                details.Add($"当前主断路器：{string.Join("、", mainBreakers.Select(n => n.Label))}");
                return Fail("MULTI_MAIN_BREAKER", "主断路器有且只能有一个",
                    details: details, problemNodeIds: mainBreakers.Select(n => n.Id).ToList());
            }

            var pccMeters = project.Nodes
                .Where(n => n.TemplateId == "ac_meter" && TopologyParamHelper.GetBool(n.Parameters, "isPccMeter"))
                .ToList();
            if (pccMeters.Count == 0)
            {
                var meters = project.Nodes.Where(n => n.TemplateId == "ac_meter").Select(n => n.Id).ToList();
                details.Add("未指定并网点电表（请在三相电表属性中勾选「作为并网点电表」）");
                return Fail("NEED_PCC_METER", "须指定有且仅有一个并网点电表",
                    details: details, problemNodeIds: meters);
            }
            if (pccMeters.Count > 1)
            {
                details.Add($"当前并网点电表：{string.Join("、", pccMeters.Select(n => n.Label))}");
                return Fail("MULTI_PCC_METER", "并网点电表有且只能有一个",
                    details: details, problemNodeIds: pccMeters.Select(n => n.Id).ToList());
            }

            // PCS 归属：每台 PCS 的 emuId 须指向工程内存在的 EMU 虚拟节点
            var emuIds = project.Nodes.Where(n => n.TemplateId == "emu").Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
            var orphanPcs = project.Nodes
                .Where(n => n.TemplateId == "pcs")
                .Where(n => !emuIds.Contains(TopologyParamHelper.GetString(n.Parameters, "emuId")))
                .ToList();
            if (orphanPcs.Count > 0)
            {
                details.Add($"以下 PCS 未选择有效的所属 EMU 储能单元：{string.Join("、", orphanPcs.Select(n => n.Label))}");
                return Fail("PCS_EMU_UNASSIGNED", "每台 PCS 变流器须在参数中选择所属 EMU 储能单元",
                    details: details, problemNodeIds: orphanPcs.Select(n => n.Id).ToList());
            }

            // 至少一个含 PCS 的 EMU 或光伏单元
            var emusWithPcs = project.Nodes
                .Where(n => n.TemplateId == "emu")
                .Where(e => project.Nodes.Any(p =>
                    p.TemplateId == "pcs" &&
                    TopologyParamHelper.GetString(p.Parameters, "emuId") == e.Id))
                .ToList();
            var pvUnits = project.Nodes.Where(n => n.TemplateId == "pv_unit").ToList();
            if (emusWithPcs.Count == 0 && pvUnits.Count == 0)
            {
                details.Add("工程中至少需要一个含 PCS 的 EMU 储能单元或光伏单元");
                return Fail("NO_GENERATION_UNIT", "工程中至少需要一个含 PCS 的 EMU 储能单元或光伏单元",
                    details: details, problemNodeIds: project.Nodes.Select(n => n.Id).ToList());
            }

            var work = CloneProject(project);
            work.Edges.Clear();
            RefreshAcBusEnergization(work);

            foreach (var edge in OrderEdgesTopToBottom(project))
            {
                var replay = CloneEdge(edge);
                replay.Id = string.IsNullOrWhiteSpace(edge.Id) ? replay.Id : edge.Id;
                var rule = ValidateConnectionRules(work, replay);
                if (!rule.Ok)
                {
                    if (rule.Details.Count == 0)
                        rule.Details.Add(rule.Message);
                    if (rule.ProblemNodeIds.Count == 0)
                    {
                        rule.ProblemNodeIds = new List<string> { edge.FromNodeId, edge.ToNodeId }
                            .Distinct(StringComparer.Ordinal)
                            .ToList();
                    }
                    return rule;
                }

                ApplyConnect(work, replay);
            }

            return new TopologyValidationResult
            {
                Ok = true,
                Message = "工程配置校验通过",
                Details =
                {
                    $"电网：{grids[0].Label}",
                    $"主断路器：{mainBreakers[0].Label}",
                    $"并网点电表：{pccMeters[0].Label}"
                }
            };
        }

        private static IEnumerable<TopologyEdge> OrderEdgesTopToBottom(TopologyProject project)
        {
            double YOf(string id) => FindNode(project, id)?.Y ?? 0;
            double XOf(string id) => FindNode(project, id)?.X ?? 0;
            return project.Edges
                .OrderBy(e => Math.Min(YOf(e.FromNodeId), YOf(e.ToNodeId)))
                .ThenBy(e => Math.Min(XOf(e.FromNodeId), XOf(e.ToNodeId)))
                .ThenBy(e => e.FromNodeId, StringComparer.Ordinal)
                .ThenBy(e => e.FromPortId, StringComparer.Ordinal)
                .ThenBy(e => e.ToNodeId, StringComparer.Ordinal)
                .ThenBy(e => e.ToPortId, StringComparer.Ordinal)
                .ThenBy(e => e.Id, StringComparer.Ordinal);
        }

        private static int PhaseOrder(string? phase) => phase?.ToUpperInvariant() switch
        {
            "A" => 0,
            "B" => 1,
            "C" => 2,
            _ => 9
        };

        private static (TopologyPortDef pos, TopologyPortDef neg)? FindDcPair(TopologyTemplate tpl, TopologyPortDef seed)
        {
            var sidePorts = tpl.Ports
                .Where(p => IsDc(p.Kind) &&
                            string.Equals(p.Side, seed.Side, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var pos = sidePorts.FirstOrDefault(p => p.Kind is "dc_pos" or "dc");
            var neg = sidePorts.FirstOrDefault(p => p.Kind == "dc_neg");
            if (pos == null || neg == null) return null;
            return (pos, neg);
        }

        private static TopologyEdge CloneEdge(TopologyEdge e) => new()
        {
            Id = string.IsNullOrWhiteSpace(e.Id) ? Guid.NewGuid().ToString("N") : e.Id,
            FromNodeId = e.FromNodeId,
            FromPortId = e.FromPortId,
            ToNodeId = e.ToNodeId,
            ToPortId = e.ToPortId
        };

        internal static TopologyProject CloneProject(TopologyProject project)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(project);
            return System.Text.Json.JsonSerializer.Deserialize<TopologyProject>(json)
                   ?? new TopologyProject();
        }

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

        private static TopologyValidationResult Fail(
            string code,
            string message,
            string? rejectEdgeId = null,
            List<string>? details = null,
            List<string>? problemNodeIds = null) =>
            new()
            {
                Ok = false,
                Code = code,
                Message = message,
                RejectEdgeId = rejectEdgeId,
                Details = details ?? new List<string>(),
                ProblemNodeIds = problemNodeIds ?? new List<string>()
            };
    }
}

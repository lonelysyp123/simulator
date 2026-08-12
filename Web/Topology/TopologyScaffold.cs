namespace EssSimulator.Web.Topology
{
    /// <summary>标准径向拓扑骨架：电网→主断→HV 母线→主变→LV 母线→N×EMU(+DC+双 BMS)。</summary>
    public static class TopologyScaffold
    {
        public const int MinEmuCount = 1;
        public const int MaxEmuCount = 20;

        public static TopologyProject BuildRadial(int emuCount, string? name = null, bool includeLoad = true)
        {
            if (emuCount < MinEmuCount || emuCount > MaxEmuCount)
                throw new ArgumentOutOfRangeException(nameof(emuCount), $"EMU 数量须在 {MinEmuCount}–{MaxEmuCount} 之间");

            var project = new TopologyProject
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                Name = string.IsNullOrWhiteSpace(name) ? $"标准径向-{emuCount}单元" : name.Trim(),
                SchemaVersion = "1.0"
            };

            var cx = 280.0;
            var grid = Add(project, "grid", "电网", cx - 20, 20);
            var brk = Add(project, "ac_breaker", "主断路器", cx + 40, 120,
                new Dictionary<string, object?> { ["isMainBreaker"] = true, ["ratedVoltage"] = 220000d, ["closed"] = true });
            var busHv = Add(project, "ac_bus", "220kV母线", cx - 50, 280,
                new Dictionary<string, object?> { ["name"] = "220kV母线", ["nominalVoltage"] = 220000d });
            var meter = Add(project, "ac_meter", "并网点电表", cx + 200, 300,
                new Dictionary<string, object?> { ["isPccMeter"] = true, ["ptPrimaryVoltage"] = 220000d });
            var xfmr = Add(project, "transformer", "主变压器", cx - 10, 380,
                new Dictionary<string, object?>
                {
                    ["primaryVoltage"] = 220000d,
                    ["secondaryVoltage"] = 35000d
                });
            var busLv = Add(project, "ac_bus", "35kV母线", cx - 50, 560,
                new Dictionary<string, object?> { ["name"] = "35kV母线", ["nominalVoltage"] = 35000d });

            TopologyNode? load = null;
            if (includeLoad)
            {
                load = Add(project, "load", "站用负载", cx + 220, 580,
                    new Dictionary<string, object?> { ["ratedVoltage"] = 35000d });
            }

            MustConnect(project, grid, "a", brk, "a");
            MustConnect(project, brk, "a2", busHv, "a");
            MustConnect(project, meter, "pt_a", busHv, "a2");
            MustConnect(project, xfmr, "pri_a", busHv, "a2");
            MustConnect(project, xfmr, "sec_a", busLv, "a");
            if (load != null)
                MustConnect(project, load, "a", busLv, "a2");

            var unitSpan = 280.0;
            var startX = cx - (emuCount - 1) * unitSpan / 2.0;
            for (var i = 0; i < emuCount; i++)
            {
                var x = startX + i * unitSpan;
                var emu = Add(project, "emu", $"EMU-{i + 1}", x, 720);
                var dc = Add(project, "dc_bus", $"DC母线-{i + 1}", x - 10, 880);
                var bmsA = Add(project, "bms", $"BMS-{i + 1}A", x - 70, 1000);
                var bmsB = Add(project, "bms", $"BMS-{i + 1}B", x + 70, 1000);

                MustConnect(project, emu, "ac_a", busLv, "a2");
                MustConnect(project, emu, "dc_pos", dc, "pos_t");
                MustConnect(project, bmsA, "dc_pos", dc, "pos_b");
                MustConnect(project, bmsB, "dc_pos", dc, "pos_b");
            }

            TopologyValidator.RefreshAcBusEnergization(project);
            project.UpdatedAtUtc = DateTime.UtcNow;
            return project;
        }

        private static TopologyNode Add(
            TopologyProject project,
            string templateId,
            string label,
            double x,
            double y,
            Dictionary<string, object?>? overrides = null)
        {
            var tpl = TopologyTemplates.Get(templateId)
                      ?? throw new InvalidOperationException($"未知模板: {templateId}");
            var parameters = new Dictionary<string, object?>(tpl.DefaultParameters);
            if (overrides != null)
            {
                foreach (var kv in overrides)
                    parameters[kv.Key] = kv.Value;
            }

            var node = new TopologyNode
            {
                Id = Guid.NewGuid().ToString("N"),
                TemplateId = templateId,
                Label = label,
                X = Snap(x),
                Y = Snap(y),
                Parameters = parameters
            };
            project.Nodes.Add(node);
            return node;
        }

        private static void MustConnect(
            TopologyProject project,
            TopologyNode from,
            string fromPort,
            TopologyNode to,
            string toPort)
        {
            var seed = new TopologyEdge
            {
                Id = Guid.NewGuid().ToString("N"),
                FromNodeId = from.Id,
                FromPortId = fromPort,
                ToNodeId = to.Id,
                ToPortId = toPort
            };
            var result = TopologyValidator.TryConnectBundle(project, seed, out var updated);
            if (!result.Ok || updated == null)
                throw new InvalidOperationException(
                    $"骨架连线失败 {from.Label}.{fromPort}→{to.Label}.{toPort}: [{result.Code}] {result.Message}");

            project.Nodes = updated.Nodes;
            project.Edges = updated.Edges;
        }

        private static double Snap(double v) => Math.Round(v / 20.0) * 20.0;
    }
}

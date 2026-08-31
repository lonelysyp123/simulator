using EssSimulator.EssDeviceSimModel.Model;

namespace EssSimulator.Web.Topology
{
    /// <summary>
    /// 把组态工程的母线/电表连线映射到运行时母线 Id，供电表采样与串联设备交换使用。
    /// </summary>
    public static class TopologyElectricalMapper
    {
        public sealed class Mapping
        {
            public IReadOnlyDictionary<string, string> BusRuntimeIds { get; init; } =
                new Dictionary<string, string>();
            public bool HasStationTransformer { get; init; }
        }

        public static Mapping Map(TopologyProject project)
        {
            var busIds = new Dictionary<string, string>(StringComparer.Ordinal);
            bool hasStationXfmr = project.Nodes.Any(IsStationTransformer);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            int unit690 = 0;

            foreach (var grid in project.Nodes.Where(n => n.TemplateId == "grid"))
                Walk(project, grid.Id, cameFrom: null, xfmrCrossed: 0, passedMainBreaker: false);

            return new Mapping
            {
                BusRuntimeIds = busIds,
                HasStationTransformer = hasStationXfmr
            };

            void Walk(
                TopologyProject p,
                string nodeId,
                string? cameFrom,
                int xfmrCrossed,
                bool passedMainBreaker)
            {
                if (!visited.Add(nodeId))
                    return;

                var node = p.Nodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null)
                    return;

                if (node.TemplateId == "ac_bus")
                {
                    if (!busIds.ContainsKey(node.Id))
                        busIds[node.Id] = AssignBusId(xfmrCrossed, passedMainBreaker, hasStationXfmr, ref unit690);

                    foreach (var nb in Neighbors(p, node.Id))
                    {
                        if (nb == cameFrom) continue;
                        Walk(p, nb, node.Id, xfmrCrossed, passedMainBreaker);
                    }
                    return;
                }

                if (node.TemplateId is "ac_meter" or "load" or "pcs" or "pv_unit"
                    or "bms" or "dc_bus" or "emu" or "emu_group")
                    return;

                bool nextMain = passedMainBreaker
                    || (node.TemplateId == "ac_breaker"
                        && TopologyParamHelper.GetBool(node.Parameters, "isMainBreaker"));
                int nextXfmr = xfmrCrossed + (node.TemplateId == "transformer" ? 1 : 0);

                foreach (var nb in Neighbors(p, node.Id))
                {
                    if (nb == cameFrom) continue;
                    Walk(p, nb, node.Id, nextXfmr, nextMain);
                }
            }
        }

        public static string? ResolveMeterSourceBusId(TopologyProject project, TopologyNode meter)
        {
            var mapping = Map(project);
            var bus = FindConnectedAcBus(project, meter.Id);
            if (bus == null)
                return null;
            return mapping.BusRuntimeIds.TryGetValue(bus.Id, out var runtimeId) ? runtimeId : null;
        }

        public static TopologyNode? FindConnectedAcBus(TopologyProject project, string nodeId)
        {
            foreach (var nbId in Neighbors(project, nodeId))
            {
                var nb = project.Nodes.FirstOrDefault(n => n.Id == nbId);
                if (nb?.TemplateId == "ac_bus")
                    return nb;
            }
            return null;
        }

        public static bool HasStationTransformer(TopologyProject project) =>
            project.Nodes.Any(IsStationTransformer);

        private static bool IsStationTransformer(TopologyNode n) =>
            n.TemplateId == "transformer"
            && string.IsNullOrWhiteSpace(TopologyParamHelper.GetString(n.Parameters, "emuId"));

        private static string AssignBusId(
            int xfmrCrossed,
            bool passedMainBreaker,
            bool hasStationXfmr,
            ref int unit690)
        {
            if (xfmrCrossed <= 0)
            {
                if (!passedMainBreaker)
                    return RuntimeBusIds.Grid;
                return hasStationXfmr ? RuntimeBusIds.AfterMainBreaker : RuntimeBusIds.Station35;
            }

            if (xfmrCrossed == 1)
                return RuntimeBusIds.Station35;

            int idx = unit690++;
            return RuntimeBusIds.Unit690(idx);
        }

        private static IEnumerable<string> Neighbors(TopologyProject project, string nodeId)
        {
            foreach (var e in project.Edges)
            {
                if (e.FromNodeId == nodeId) yield return e.ToNodeId;
                else if (e.ToNodeId == nodeId) yield return e.FromNodeId;
            }
        }
    }
}

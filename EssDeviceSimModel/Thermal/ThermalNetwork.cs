using System;
using System.Collections.Generic;

namespace EssSimulator.EssDeviceSimModel.Thermal
{
    /// <summary>热网络节点：热容 C，温度 T，外加热源 P。</summary>
    public sealed class ThermalNode
    {
        public ThermalNode(string id, double thermalCapacityJPerK, double initialTempCelsius, bool isBoundary = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Node id required.", nameof(id));
            if (!isBoundary && thermalCapacityJPerK <= 0)
                throw new ArgumentOutOfRangeException(nameof(thermalCapacityJPerK));

            Id = id;
            ThermalCapacityJPerK = thermalCapacityJPerK;
            TemperatureCelsius = initialTempCelsius;
            IsBoundary = isBoundary;
        }

        public string Id { get; }
        public double ThermalCapacityJPerK { get; }
        public double TemperatureCelsius { get; set; }

        /// <summary>边界节点温度由外部设定，不参与积分（如室外）。</summary>
        public bool IsBoundary { get; }

        /// <summary>本步注入热功率（W），正为加热。</summary>
        public double HeatInjectionW { get; set; }
    }

    /// <summary>两节点间热导 G = 1/R（W/K）。</summary>
    public readonly struct ThermalEdge
    {
        public ThermalEdge(string nodeAId, string nodeBId, double resistanceKPerW)
        {
            if (resistanceKPerW <= 0)
                throw new ArgumentOutOfRangeException(nameof(resistanceKPerW));
            NodeAId = nodeAId;
            NodeBId = nodeBId;
            ConductanceWPerK = 1.0 / resistanceKPerW;
        }

        public string NodeAId { get; }
        public string NodeBId { get; }
        public double ConductanceWPerK { get; }
    }

    /// <summary>
    /// 集总参数热网络显式欧拉步进：
    /// C_i · dT_i/dt = P_i + Σ_j G_ij (T_j − T_i)
    /// </summary>
    public sealed class ThermalNetwork
    {
        private readonly Dictionary<string, ThermalNode> _nodes = new(StringComparer.Ordinal);
        private readonly List<ThermalEdge> _edges = new();

        public IReadOnlyDictionary<string, ThermalNode> Nodes => _nodes;

        public ThermalNode AddNode(ThermalNode node)
        {
            _nodes[node.Id] = node;
            return node;
        }

        public void AddEdge(ThermalEdge edge)
        {
            if (!_nodes.ContainsKey(edge.NodeAId) || !_nodes.ContainsKey(edge.NodeBId))
                throw new InvalidOperationException("Edge references unknown node.");
            _edges.Add(edge);
        }

        public ThermalNode GetNode(string id) => _nodes[id];

        public void Step(TimeSpan dt)
        {
            double seconds = Math.Max(1e-6, dt.TotalSeconds);
            var deltaT = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var kv in _nodes)
            {
                if (kv.Value.IsBoundary)
                    continue;
                deltaT[kv.Key] = 0;
            }

            foreach (var edge in _edges)
            {
                var a = _nodes[edge.NodeAId];
                var b = _nodes[edge.NodeBId];
                double heatFlowW = edge.ConductanceWPerK * (b.TemperatureCelsius - a.TemperatureCelsius);
                // 流入 A 为正
                if (!a.IsBoundary)
                    deltaT[a.Id] += heatFlowW;
                if (!b.IsBoundary)
                    deltaT[b.Id] -= heatFlowW;
            }

            foreach (var kv in _nodes)
            {
                var node = kv.Value;
                if (node.IsBoundary)
                {
                    node.HeatInjectionW = 0;
                    continue;
                }

                double netPowerW = deltaT[node.Id] + node.HeatInjectionW;
                node.TemperatureCelsius += netPowerW * seconds / node.ThermalCapacityJPerK;
                node.HeatInjectionW = 0;
            }
        }
    }
}

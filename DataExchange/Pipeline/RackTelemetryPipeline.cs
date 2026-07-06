using EssSimulator.DataExchange.Adapters;
using EssSimulator.DataExchange.Catalog;
using log4net;

namespace EssSimulator.DataExchange.Pipeline
{
    /// <summary>BMS Rack FC3/4：按 rack 从站写入遥测。</summary>
    public sealed class RackTelemetryPipeline
    {
        private readonly IReadOnlyList<RackPointBinding> _points;
        private readonly ISimulationDataAdapter _simulation;
        private readonly IModbusRegisterAdapter _modbus;
        private readonly int _clusterCount;
        private readonly byte _baseSlaveId;
        private readonly Dictionary<string, object?> _shadow = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILog _log = LogManager.GetLogger(typeof(RackTelemetryPipeline));

        public RackTelemetryPipeline(
            IReadOnlyList<RackPointBinding> points,
            ISimulationDataAdapter simulation,
            IModbusRegisterAdapter modbus,
            int clusterCount,
            byte baseSlaveId = 1)
        {
            _points = points;
            _simulation = simulation;
            _modbus = modbus;
            _clusterCount = clusterCount;
            _baseSlaveId = baseSlaveId;
        }

        /// <summary>清空 rack 遥测 shadow（与 bank 遥测一同在 Modbus 重连后失效）。</summary>
        public void ClearShadow() => _shadow.Clear();

        public void RunOnce()
        {
            if (_points.Count == 0 || _clusterCount <= 0)
                return;

            for (int rackId = 0; rackId < _clusterCount; rackId++)
            {
                byte slaveId = (byte)(_baseSlaveId + rackId + 1);
                var writeBuffer = new Dictionary<string, object>();

                foreach (var binding in _points)
                {
                    try
                    {
                        var path = binding.ResolvePath(rackId);
                        var value = _simulation.Read(path) ?? 0;
                        string key = $"{rackId}:{binding.ParamName}";
                        if (_shadow.TryGetValue(key, out var prev) &&
                            ShadowStore.ValuesEqual(prev, value))
                            continue;

                        _shadow[key] = value;
                        writeBuffer[binding.ParamName] = value;
                    }
                    catch (Exception ex)
                    {
                        _log.Debug($"Rack telemetry read failed: rack={rackId} {binding.ParamName}", ex);
                    }
                }

                if (writeBuffer.Count > 0)
                    _modbus.WritePoints(writeBuffer, slaveId);
            }
        }
    }
}

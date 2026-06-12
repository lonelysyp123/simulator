using EssSimulator.DataExchange.Adapters;
using EssSimulator.DataExchange.Catalog;
using log4net;

namespace EssSimulator.DataExchange.Pipeline
{
    /// <summary>FC 3/4：仿真 → Modbus，变更才写。</summary>
    public sealed class TelemetryPipeline
    {
        private readonly PointCatalog _catalog;
        private readonly ISimulationDataAdapter _simulation;
        private readonly IModbusRegisterAdapter _modbus;
        private readonly ShadowStore _shadow;
        private readonly ILog _log = LogManager.GetLogger(typeof(TelemetryPipeline));

        public TelemetryPipeline(
            PointCatalog catalog,
            ISimulationDataAdapter simulation,
            IModbusRegisterAdapter modbus,
            ShadowStore shadow)
        {
            _catalog = catalog;
            _simulation = simulation;
            _modbus = modbus;
            _shadow = shadow;
        }

        public void RunOnce()
        {
            if (_catalog.TelemetryPoints.Count == 0)
                return;

            var writeBuffer = new Dictionary<string, object>();
            foreach (var binding in _catalog.TelemetryPoints)
            {
                try
                {
                    var value = _simulation.Read(binding.Target.FullPath) ?? 0;
                    if (_shadow.TelemetryChanged(binding.ParamName, value))
                        writeBuffer[binding.ParamName] = value;
                }
                catch (Exception ex)
                {
                    _log.Debug($"Telemetry read failed: {binding.ParamName}", ex);
                }
            }

            _modbus.WritePoints(writeBuffer);
        }
    }
}

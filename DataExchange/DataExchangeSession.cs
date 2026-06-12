using EssSimulator.DataExchange.Adapters;
using EssSimulator.DataExchange.Catalog;
using EssSimulator.DataExchange.Config;
using EssSimulator.DataExchange.Effects;
using EssSimulator.DataExchange.Pipeline;
using EssSimulator.Protocol.Modbus;
using log4net;

namespace EssSimulator.DataExchange
{
    /// <summary>
    /// 点表驱动数据交互会话：遥测/控制/反馈三管道（替代 ModbusDataSync）。
    /// </summary>
    public sealed class DataExchangeSession : IModbusSyncBackend
    {
        private readonly IModbusSlave _slave;
        private readonly ModbusParser _parser;
        private readonly PointCatalog _catalog;
        private readonly DeviceInfoDto _deviceInfo;
        private readonly DataExchangeOptions _options;
        private readonly int _clusterCount;
        private readonly ShadowStore _shadow = new();
        private readonly ILog _log = LogManager.GetLogger(typeof(DataExchangeSession));

        private readonly ISimulationDataAdapter _simulation;
        private readonly TelemetryPipeline _telemetryPipeline;
        private readonly RackTelemetryPipeline? _rackTelemetryPipeline;
        private readonly ControlPipeline _controlPipeline;
        private readonly ControlFeedbackPipeline _feedbackPipeline;
        private readonly IModbusRegisterAdapter _modbusAdapter;
        private readonly ControlEffectRegistry _effects;

        private volatile bool _running;
        private Thread? _telemetryThread;
        private Thread? _rackTelemetryThread;
        private Thread? _controlThread;
        private Thread? _feedbackThread;

        public DataExchangeSession(
            IModbusSlave slave,
            ModbusParser parser,
            PointCatalog catalog,
            DeviceInfoDto deviceInfo,
            DataExchangeOptions options,
            int clusterCount = 0)
        {
            _slave = slave;
            _parser = parser;
            _catalog = catalog;
            _deviceInfo = deviceInfo;
            _options = options;
            _clusterCount = clusterCount;

            _simulation = new ReflectionSimulationAdapter();
            _modbusAdapter = new ModbusRegisterAdapter(slave, parser);

            _effects = new ControlEffectRegistry();
            string serverName = deviceInfo.name ?? string.Empty;
            if (serverName.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase))
            {
                _effects
                    .Register(new EmuPcsControlEffect())
                    .Register(new EmuUnitBreakerEffect());
            }
            else if (serverName.StartsWith("simBms", StringComparison.OrdinalIgnoreCase))
            {
                _effects.Register(new BmsLinkControlEffect());
            }

            bool logChanges = ShouldLogChanges(deviceInfo.name);

            _telemetryPipeline = new TelemetryPipeline(catalog, _simulation, _modbusAdapter, _shadow);
            if (clusterCount > 0 && catalog.RackTelemetryPoints.Count > 0)
            {
                _rackTelemetryPipeline = new RackTelemetryPipeline(
                    catalog.RackTelemetryPoints,
                    _simulation,
                    _modbusAdapter,
                    clusterCount,
                    deviceInfo.slaveId);
            }

            _controlPipeline = new ControlPipeline(
                catalog, _simulation, _modbusAdapter, parser, _shadow, _effects, serverName, logChanges);
            _feedbackPipeline = new ControlFeedbackPipeline(
                catalog, _simulation, _modbusAdapter, _shadow, serverName, logChanges);
        }

        private static bool ShouldLogChanges(string? serverName) =>
            serverName?.StartsWith("simEmu", StringComparison.OrdinalIgnoreCase) == true
            || serverName?.StartsWith("simBms", StringComparison.OrdinalIgnoreCase) == true;

        public void Start()
        {
            if (_running) return;
            _running = true;

            _modbusAdapter.WriteDefaults(_catalog.DefaultValues);
            InitializeControlRegistersFromSimulation();

            _telemetryThread = new Thread(TelemetryLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"DataExchange-Telemetry-{_deviceInfo.name}"
            };
            if (_rackTelemetryPipeline != null)
            {
                _rackTelemetryThread = new Thread(RackTelemetryLoop)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal,
                    Name = $"DataExchange-RackTelemetry-{_deviceInfo.name}"
                };
            }

            _controlThread = new Thread(ControlLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"DataExchange-Control-{_deviceInfo.name}"
            };
            _feedbackThread = new Thread(FeedbackLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"DataExchange-Feedback-{_deviceInfo.name}"
            };

            _telemetryThread.Start();
            _rackTelemetryThread?.Start();
            _controlThread.Start();
            _feedbackThread.Start();
            string rackInfo = _rackTelemetryPipeline != null ? $", rackTelemetry={_options.TelemetryIntervalMs}ms×{_clusterCount}" : string.Empty;
            _log.Info($"[DataExchange] {_deviceInfo.name} 已启动（telemetry={_options.TelemetryIntervalMs}ms, control={_options.ControlPollIntervalMs}ms, feedback={_options.ControlPollIntervalMs}ms{rackInfo}）");
        }

        public void Stop()
        {
            _running = false;
            TryJoin(_telemetryThread);
            TryJoin(_rackTelemetryThread);
            TryJoin(_controlThread);
            TryJoin(_feedbackThread);
            _telemetryThread = null;
            _rackTelemetryThread = null;
            _controlThread = null;
            _feedbackThread = null;
        }

        public void SetDataObjectByMesurePointName(string name, object value) =>
            TrySetControl(name, value, updateShadow: true);

        public void PublishControlToSlave(string name, object value)
        {
            if (!TrySetControl(name, value, updateShadow: false, out var applied))
                return;

            _feedbackPipeline.PublishImmediate(name, applied, out _);
        }

        public void InvalidateDataShadow(string name) =>
            _shadow.InvalidateTelemetry(name);

        public object? GetDataObjectByMesurePointName(string name, IModbusSlave slave, ModbusParser parser) =>
            _modbusAdapter.ReadParsedPoint(name);

        private void InitializeControlRegistersFromSimulation()
        {
            try
            {
                var initCtl = new Dictionary<string, object>();
                foreach (var binding in _catalog.ControlPoints)
                {
                    var cur = _simulation.Read(binding.Target.FullPath);
                    if (cur != null)
                    {
                        initCtl[binding.ParamName] = cur;
                        _shadow.SeedControl(binding.ParamName, cur);
                    }
                }

                if (initCtl.Count > 0)
                    _modbusAdapter.WritePoints(initCtl);
            }
            catch (Exception ex)
            {
                _log.Warn("Init control register defaults failed.", ex);
            }
        }

        private void TelemetryLoop()
        {
            while (_running)
            {
                try
                {
                    _telemetryPipeline.RunOnce();
                }
                catch (Exception ex)
                {
                    _log.Error($"Telemetry loop error [{_deviceInfo.name}]", ex);
                }

                Thread.Sleep(Math.Max(10, _options.TelemetryIntervalMs));
            }
        }

        private void RackTelemetryLoop()
        {
            if (_rackTelemetryPipeline == null)
                return;

            while (_running)
            {
                try
                {
                    _rackTelemetryPipeline.RunOnce();
                }
                catch (Exception ex)
                {
                    _log.Error($"Rack telemetry loop error [{_deviceInfo.name}]", ex);
                }

                Thread.Sleep(Math.Max(10, _options.TelemetryIntervalMs));
            }
        }

        private void ControlLoop()
        {
            while (_running)
            {
                try
                {
                    _controlPipeline.RunOnce();
                }
                catch (Exception ex)
                {
                    _log.Error($"Control loop error [{_deviceInfo.name}]", ex);
                }

                Thread.Sleep(Math.Max(10, _options.ControlPollIntervalMs));
            }
        }

        private void FeedbackLoop()
        {
            while (_running)
            {
                try
                {
                    _feedbackPipeline.RunOnce();
                }
                catch (Exception ex)
                {
                    _log.Error($"Feedback loop error [{_deviceInfo.name}]", ex);
                }

                Thread.Sleep(Math.Max(10, _options.ControlPollIntervalMs));
            }
        }

        private bool TrySetControl(string name, object value, bool updateShadow) =>
            TrySetControl(name, value, updateShadow, out _);

        private bool TrySetControl(string name, object value, bool updateShadow, out object appliedValue)
        {
            appliedValue = value;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var binding = _catalog.FindControl(name);
            if (binding == null)
                return false;

            appliedValue = CoerceForTarget(_simulation, binding, value);
            if (!_simulation.Write(binding.Target.FullPath, appliedValue))
            {
                _log.Warn($"SetExtIfVariableVal failed: {binding.Target.FullPath} <= {appliedValue}");
                return false;
            }

            if (updateShadow)
                _shadow.CommitControl(name, appliedValue);

            if (binding.Effect != ControlEffectId.None)
            {
                _effects.Dispatch(binding.Effect, new ControlEffectContext
                {
                    ServerName = _deviceInfo.name ?? string.Empty,
                    Binding = binding,
                    AppliedValue = appliedValue,
                    PreviousValue = null
                });
            }

            return true;
        }

        private static object CoerceForTarget(
            ISimulationDataAdapter simulation,
            PointBinding binding,
            object valToSet)
        {
            if (valToSet is string s)
            {
                if (double.TryParse(s, out var dv))
                    valToSet = dv;
                else if (bool.TryParse(s, out var bv))
                    valToSet = bv ? 1 : 0;
            }

            if (binding.Entry.FunctionCode != 5)
                return valToSet;

            bool coilBool = valToSet switch
            {
                bool b => b,
                string str when bool.TryParse(str, out var bv) => bv,
                _ => Convert.ToDouble(valToSet) != 0
            };

            var current = simulation.Read(binding.Target.FullPath);
            if (current is bool)
                return coilBool;

            return coilBool ? 1 : 0;
        }

        private static void TryJoin(Thread? thread)
        {
            if (thread == null || !thread.IsAlive)
                return;

            try { thread.Join(2000); }
            catch { /* ignore */ }
        }

        private static string FormatValue(object? value) =>
            value == null ? "<null>" : value.ToString() ?? "<null>";
    }
}

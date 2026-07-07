using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssDeviceSimModel
{
    using EssSimulator.Configuration;
    using EssSimulator.EssDeviceSimModel.Battery;
    using EssSimulator.EssDeviceSimModel.Devices;
    using EssSimulator.EssDeviceSimModel.Model;
    using EssSimulator.EssDeviceSimModel.Propagation;
    using EssSimulator.EssDeviceSimModel.Solver;
    using EssSimulator.EssSimModelApi;
    using EssSimulator.EssSimModelApi.BatteryManagementSystem;
    using System;
    using System.Collections.Generic;

    public class EnergyStorageSystem : BackgroundService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EnergyStorageSystem));
        // 储能系统参数
        public double Capacity { get; private set; } // 储能容量 (kWh)
        public double CurrentEnergy { get; private set; } // 当前储能 (kWh)
        public double Efficiency { get; private set; } // 充放电效率 (0-1)

        // 统计数据
        public double TotalChargeEnergy { get; private set; } // 总充电能量 (kWh)
        public double TotalDischargeEnergy { get; private set; } // 总放电能量 (kWh)
        public List<double> ChargeSessions { get; private set; } // 单次充电能量记录
        public List<double> DischargeSessions { get; private set; } // 单次放电能量记录
        public Dictionary<DateTime, double> DailyCharge { get; private set; } // 日充电能量
        public Dictionary<DateTime, double> DailyDischarge { get; private set; } // 日放电能量
        public double AvailableChargeEnergy => Capacity - CurrentEnergy; // 可获得充电能量
        public double AvailableDischargeEnergy => CurrentEnergy; // 可获得放电能量

        // 当前充电/放电状态


        /// <summary>电池堆列表，索引 i 对应第 i+1 个储能单元。通过 ess._batteryRacks[i] 或路径 ess._batteryRacks[0] 访问。</summary>
        public IReadOnlyList<BatteryRackSimulator> _batteryRacks { get; }

        /// <summary>新电气网络 BMS 设备（与 _batteryRacks 一一对应）。</summary>
        public IReadOnlyList<BmsRackDevice> _bmsRackDevices { get; }

        /// <summary>PCS 列表，索引 i 对应第 i+1 个 PCS。与电气网络 PcsDevices 共用实例。</summary>
        public IReadOnlyList<PcsDevice> _pcsList { get; }

        /// <summary>兼容旧路径：ess._batteryRack 等价于 ess._batteryRacks[0]</summary>
        [Obsolete("请使用 _batteryRacks[0]")]
        public BatteryRackSimulator _batteryRack => _batteryRacks.Count > 0 ? _batteryRacks[0] : null!;

        /// <summary>兼容旧路径：ess._batteryRack2 等价于 ess._batteryRacks[1]</summary>
        [Obsolete("请使用 _batteryRacks[1]")]
        public BatteryRackSimulator _batteryRack2 => _batteryRacks.Count > 1 ? _batteryRacks[1] : null!;

        /// <summary>兼容旧路径：ess._pcs1 等价于 ess._pcsList[0]</summary>
        [Obsolete("请使用 _pcsList[0]")]
        public PcsDevice _pcs1 => _pcsList.Count > 0 ? _pcsList[0] : null!;

        /// <summary>兼容旧路径：ess._pcs2 等价于 ess._pcsList[1]</summary>
        [Obsolete("请使用 _pcsList[1]")]
        public PcsDevice _pcs2 => _pcsList.Count > 1 ? _pcsList[1] : null!;

        //public GridState _gridState;
        public Breaker _breaker { get; set; } //断路器
        public IReadOnlyList<Breaker> _unitBreakers { get; }                 // 单元变前高压断路器（每个 Unit 1 个）
        public TransformerDevice _mainTransformer { get; set; }              // 220kV/35kV 主变
        public IReadOnlyList<TransformerDevice> _unitTransformers { get; }   // 35kV/690V 单元变（每个 Unit 1 台）
        /// <summary>35kV 负载设备（与电气网络 Load 共用实例）。</summary>
        public LoadDevice _loadDevice { get; private set; }

        /// <summary>兼容 GUI 路径 ess._loadSimulator.ActivePower。</summary>
        public LoadDevice _loadSimulator => _loadDevice;

        /// <summary>220kV 并网点（PCC）线电压（V），与并网电表、无功调压闭环一致。</summary>
        public double PccLineVoltageV { get; private set; }

        /// <summary>35kV 站内母线线电压（V），由 PCC 电压按额定变比推导。</summary>
        public double StationBus35LineVoltageV { get; private set; }

        /// <summary>新电气网络（阶段 4 起始终初始化）。</summary>
        public ElectricalNetwork ElectricalNetwork => _electricalNetwork;

        /// <summary>电气输出订阅路由（设备间互相注册）。</summary>
        public ElectricalSignalRouter SignalRouter { get; }

        /// <summary>径向前推回代引擎（<see cref="Configuration.RuntimeConfig.UseElectricalPropagation"/> 为 true 时非空）。</summary>
        public RadialPowerSweepEngine? PowerSweepEngine { get; }

        /// <summary>径向网络母线图。</summary>
        public RadialNetworkGraph? RadialGraph { get; }

        private readonly ElectricalNetwork _electricalNetwork;
        private readonly bool _useElectricalPropagation;
        private readonly int _propagationIntervalMs;

        public EnergyStorageSystem(
            SimulatorConfig simCfg,
            PcsPhysicalConfig pcsCfg,
            TransformerConfig transCfg,
            UnitTransformerConfig unitTransCfg,
            LoadConfig loadCfg,
            PccConfig pccCfg,
            MeterConfig meterCfg)
        {
            var racks = new List<BatteryRackSimulator>();
            var bmsRackDevices = new List<BmsRackDevice>();
            var pcsList = new List<PcsDevice>();
            var bmsDeviceConfigs = simCfg.GetBmsDeviceConfigs();
            var pcsDeviceConfigs = simCfg.GetPcsDeviceConfigs();
            int channelCount = Math.Max(1, bmsDeviceConfigs.Count); // PCS/BMS 通道数（= Unit*2）
            int unitCount = Math.Max(1, simCfg.Devices?.Count ?? 1); // 储能单元数（每单元2路PCS+2路BMS）

            for (int i = 0; i < channelCount; i++)
            {
                var bmsCfg = bmsDeviceConfigs[i];
                var rack = BmsRackFactory.CreateRack(bmsCfg);
                racks.Add(rack);
                int u = i / 2;
                int ch = i % 2;
                var bmsDev = new BmsRackDevice($"bms_u{u}_ch{ch}", rack);
                bmsDev.DisplayLabel = $"bms{i + 1}";
                bmsRackDevices.Add(bmsDev);
            }

            for (int i = 0; i < channelCount; i++)
            {
                var pcsDeviceCfg = i < pcsDeviceConfigs.Count ? pcsDeviceConfigs[i] : new Configuration.PcsDeviceConfig();
                var rampCfg = pcsDeviceCfg.PcsRamp ?? simCfg.Runtime.PcsRamp;
                var cfg = PcsDeviceFactory.CreateConfig(pcsCfg, rampCfg);
                int u = i / 2;
                int ch = i % 2;
                var pcs = PcsDeviceFactory.Create($"pcs_u{u}_ch{ch}", cfg);
                pcs.DisplayLabel = $"pcs{i + 1}";
                pcsList.Add(pcs);
            }

            _batteryRacks = racks;
            _bmsRackDevices = bmsRackDevices;
            _pcsList      = pcsList;

            _breaker = new Breaker();

            // 单元断路器（默认合闸，允许通过 emu.poweronoff 控制）
            var unitBreakers = new List<Breaker>();
            for (int u = 0; u < unitCount; u++)
            {
                var brk = new Breaker();
                brk.IsClosed = true;
                unitBreakers.Add(brk);
            }
            _unitBreakers = unitBreakers;

            _mainTransformer = TransformerDeviceFactory.Create(
                "main_transformer",
                TransformerDeviceFactory.CreateConfig(transCfg));

            var unitTransformers = new List<TransformerDevice>();
            var unitTransDeviceCfg = TransformerDeviceFactory.CreateConfig(unitTransCfg);
            for (int u = 0; u < unitCount; u++)
                unitTransformers.Add(TransformerDeviceFactory.Create($"unit_transformer_u{u}", unitTransDeviceCfg));
            _unitTransformers = unitTransformers;

            _loadDevice = LoadDeviceFactory.Create("load_35", loadCfg);

            // 初始化统计数据
            TotalChargeEnergy   = 0;
            TotalDischargeEnergy = 0;
            ChargeSessions    = new List<double>();
            DischargeSessions = new List<double>();
            DailyCharge    = new Dictionary<DateTime, double>();
            DailyDischarge = new Dictionary<DateTime, double>();

            // 保存仿真时钟参数
            double integrationMult = simCfg.IntegrationStepMultiplier;
            _integrationMultiplier = integrationMult > 0 ? integrationMult : 1.0;
            _lastCycleUtc = DateTime.UtcNow;
            _transCfg  = transCfg;
            _pcsCfg    = pcsCfg;
            _useElectricalPropagation = simCfg.Runtime.UseElectricalPropagation;
            _propagationIntervalMs = Math.Max(10, simCfg.Runtime.PropagationIntervalMs);
            PccLineVoltageV = pccCfg.NominalLineVoltage;
            StationBus35LineVoltageV = pccCfg.StationBusNominalLineVoltage;

            SignalRouter = new ElectricalSignalRouter();

            _electricalNetwork = NetworkTopologyBuilder.Build(
                simCfg, pcsCfg, transCfg, unitTransCfg, loadCfg, pccCfg,
                meterCfg: meterCfg,
                bmsRackDevices: _bmsRackDevices,
                externalPcsDevices: pcsList,
                externalMainTransformer: _mainTransformer,
                externalUnitTransformers: unitTransformers,
                externalLoadDevice: _loadDevice,
                legacyEss: this);

            if (_useElectricalPropagation)
            {
                RadialGraph = new RadialNetworkGraph(_electricalNetwork, pccCfg, pcsCfg);
                PowerSweepEngine = new RadialPowerSweepEngine(
                    RadialGraph,
                    this,
                    pccCfg,
                    pcsCfg,
                    simCfg.Runtime.PropagationQuvMaxIterations,
                    simCfg.Runtime.PropagationVoltageTolerancePu);
                _log.Info($"[EnergyStorageSystem] 母线前推回代已启用（{_propagationIntervalMs} ms）");
            }
            else
            {
                _log.Info("[EnergyStorageSystem] 电气网络 Solver 主路径已启用");
            }
        }

        /// <summary>阶段 4 起固定为 Solver 主路径 + 网络控制面。</summary>
        public bool UsesNetworkControlPlane => _electricalNetwork != null;

        internal void ApplyNetworkGridVoltages(double pccLineVoltageV, double stationBus35LineVoltageV)
        {
            PccLineVoltageV = pccLineVoltageV;
            StationBus35LineVoltageV = stationBus35LineVoltageV;
        }

        /// <summary>主断路器是否合闸（以电气网络为准）。</summary>
        public bool IsMainBreakerClosed =>
            NetworkControlBridge.IsBreakerClosed(_electricalNetwork.MainBreaker);

        /// <summary>单元高压断路器是否合闸（以电气网络为准）。</summary>
        public bool IsUnitBreakerClosed(int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= _unitBreakers.Count)
                return false;

            if (unitIndex < _electricalNetwork.UnitBreakers.Count)
                return NetworkControlBridge.IsBreakerClosed(_electricalNetwork.UnitBreakers[unitIndex]);

            return _unitBreakers[unitIndex].IsClosed;
        }

        /// <summary>设定主断路器合/分（写入电气网络并投影至 Legacy）。</summary>
        public void SetMainBreakerClosed(bool closed) =>
            NetworkControlBridge.ApplyMainBreakerClosed(_electricalNetwork, _breaker, _loadDevice, closed);

        /// <summary>设定单元高压断路器合/分（写入电气网络并投影至 Legacy）。</summary>
        public void SetUnitBreakerClosed(int unitIndex, bool closed) =>
            NetworkControlBridge.ApplyUnitBreakerClosed(_electricalNetwork, _unitBreakers, unitIndex, closed);

        /// <summary>设定负载计划并同步至电气网络 Load 设备。</summary>
        public void SetLoadCharacteristic(string characteristic, double value) =>
            _loadDevice.SetLoadCharacteristic(characteristic, value);

        /// <summary>运行时设定仿真电网额定线电压（V）。仅主断闭合时对外体现为 PCC 电压基准。</summary>
        public bool TrySetGridVoltage(double lineVoltageV, out string message)
        {
            message = string.Empty;
            if (lineVoltageV <= 0)
            {
                message = "电网电压必须大于 0（V，例如 220000）";
                return false;
            }

            if (lineVoltageV > 1_000_000)
            {
                message = "电网电压超出合理范围（≤ 1000000 V）";
                return false;
            }

            try
            {
                _electricalNetwork.Grid.SetNominalLineVoltage(lineVoltageV);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                message = ex.Message;
                return false;
            }

            message = $"电网额定线电压 = {lineVoltageV} V（主断闭合后 PCC 在此基础上叠加 Q-U 偏移）";
            return true;
        }

        /// <summary>运行时设定仿真电网额定频率（Hz）。并网时 PCS 跟网、电表频率取此值。</summary>
        public bool TrySetGridFrequency(double frequencyHz, out string message)
        {
            message = string.Empty;
            if (frequencyHz <= 0 || frequencyHz > 75)
            {
                message = "电网频率须在 (0, 75] Hz";
                return false;
            }

            try
            {
                _electricalNetwork.Grid.SetNominalFrequency(frequencyHz);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                message = ex.Message;
                return false;
            }

            message = $"电网额定频率 = {frequencyHz} Hz（主断闭合后 PCS 跟网、simEm.yc19 反映此值）";
            return true;
        }

        /// <summary>写入 PCS 黑启动开关（含断路器联锁）；违规则返回 false 且不开启。</summary>
        public bool TrySetPcsBlackStart(int pcsSimIndex, bool requested)
        {
            if (pcsSimIndex < 0 || pcsSimIndex >= _pcsList.Count)
                return false;

            if (requested && BlackStartInterlock.IsStationShortCircuitRisk(this, pcsSimIndex, true))
                return false;

            _pcsList[pcsSimIndex].ApplyBlackStartEnabled(requested);
            return true;
        }

        /// <summary>扫描全部 PCS 黑启动联锁；违规时回调 pcsNumber（1-based）。</summary>
        public void ValidatePcsBlackStartInterlocks(Action<int> onViolation)
        {
            for (int i = 0; i < _pcsList.Count; i++)
            {
                if (!BlackStartInterlock.IsStationShortCircuitRisk(
                        this, i, _pcsList[i].GetCurrentState().BlackStartEnabled))
                    continue;

                onViolation(i + 1);
                return;
            }
        }

        /// <summary>EMU/控制面变更后刷新网络 PCS 端口（与 _pcsList 共用实例，无需重复同步）。</summary>
        public void PushPcsChannelToNetwork(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= _pcsList.Count)
                return;
            _pcsList[channelIndex].Step(new DeviceStepContext(), TimeSpan.Zero);
        }

        /// <summary>BMS 并网链路（Rack + DcLink + BmsRackDevice）。</summary>
        public void SetBmsPcsLinked(int channelIndex, bool linked)
        {
            if (channelIndex < 0 || channelIndex >= _bmsRackDevices.Count)
                return;
            NetworkControlBridge.SetBmsPcsLinked(_electricalNetwork, _bmsRackDevices[channelIndex], channelIndex, linked);
        }

        // 仿真时钟：每次主循环 / 电压源 SolveCycle 回调时，用真实墙钟间隔作为 dt
        private DateTime _lastCycleUtc;
        private readonly double _integrationMultiplier;

        /// <summary>电压源（Grid）激活周期（ms），主循环休眠间隔。</summary>
        public int LoopIntervalMs => _propagationIntervalMs;

        private (TimeSpan elapsed, TimeSpan integrationElapsed) AdvanceCycleClock()
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastCycleUtc;
            _lastCycleUtc = now;

            if (elapsed <= TimeSpan.Zero)
                elapsed = TimeSpan.FromMilliseconds(1);

            var integrationElapsed = TimeSpan.FromTicks((long)(elapsed.Ticks * _integrationMultiplier));
            return (elapsed, integrationElapsed);
        }

        private readonly TransformerConfig _transCfg;
        private readonly PcsPhysicalConfig _pcsCfg;

        public string GetBlackStartSteadyLossShareMode() => _pcsCfg.BlackStartSteadyLossShareMode;

        /// <summary>
        /// 仿真主循环（IHostedService / BackgroundService）。
        /// 由 .NET Host 在 StartAsync 时调用，stoppingToken 取消时自动退出。
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.Info("[EnergyStorageSystem] 仿真主循环启动");
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_propagationIntervalMs));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    var (elapsed, integrationElapsed) = AdvanceCycleClock();
                    DateTime simTime = DateTime.UtcNow;

                    if (_useElectricalPropagation && PowerSweepEngine != null)
                    {
                        PowerSweepEngine.SolveCycle(simTime, elapsed, integrationElapsed);
                    }
                    else
                    {
                        NetworkStepOrchestrator.SolverPrimaryStep(
                            _electricalNetwork, this, simTime, elapsed, integrationElapsed, _pcsCfg);
                    }

                    Update(simTime, elapsed, integrationElapsed);
                    SyncUnitTransformerAfterPcsUpdate(simTime, elapsed);
                    RefreshAllUnitBlackStartBusContexts();
                    NetworkControlBridge.SyncBmsLinksFromRacks(_electricalNetwork, _bmsRackDevices);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止，不视为错误
                _log.Info("[EnergyStorageSystem] 仿真主循环收到停止信号，正常退出");
            }
            catch (Exception ex)
            {
                _log.Fatal("[EnergyStorageSystem] 仿真主循环发生未处理异常，已停止", ex);
                throw; // 重新抛出，让 Host 感知到服务崩溃
            }
        }

        /// <summary>
        /// 单元/主网侧无电：非黑启动则停机；黑启动保留离网建压（网侧不可用，由 EMS 启停+黑启动驱动）。
        /// </summary>
        internal void ApplyPcsGridWhenUnitDeenergized(int pcsSimIndex, PcsDevice pcs)
        {
            int unit = pcsSimIndex / 2;
            double busV = GetUnitAcBusVoltage(unit);
            double energizedV = _pcsCfg.AcVoltageNominal * _pcsCfg.BlackStartBusEnergizedFraction;
            if (busV >= energizedV)
            {
                pcs.UpdateGridState(busV, _electricalNetwork.SystemFrequencyHz, false);
                return;
            }

            if (pcs.GetCurrentState().BlackStartEnabled)
            {
                pcs.UpdateGridState(0, _electricalNetwork.SystemFrequencyHz, false);
                return;
            }

            pcs.UpdateGridState(0, 0, false);
            pcs.ApplyBlackStartEnabled(false);
            pcs.TransitionToMode(OperationMode.Off, "单元/主网侧无电且非黑启动");
        }


        /// <summary>同单元 690V 母线电压（单元变二次侧与各 PCS 交流电压取大）。</summary>
        public double GetUnitAcBusVoltage(int unitIndex)
        {
            double v = 0;
            int a = unitIndex * 2;
            int b = a + 1;
            if (unitIndex >= 0 && unitIndex < _unitTransformers.Count)
                v = Math.Max(v, _unitTransformers[unitIndex].GetCurrentState().SecondaryVoltage);
            if (a >= 0 && a < _pcsList.Count)
                v = Math.Max(v, _pcsList[a].GetCurrentState().AcVoltage);
            if (b >= 0 && b < _pcsList.Count)
                v = Math.Max(v, _pcsList[b].GetCurrentState().AcVoltage);
            return v;
        }

        private void RefreshUnitBlackStartBusContext(int unitIndex)
        {
            double busV = GetUnitAcBusVoltage(unitIndex);
            int a = unitIndex * 2;
            int b = a + 1;
            if (a >= 0 && a < _pcsList.Count)
                _pcsList[a].RefreshBlackStartBusContext(busV);
            if (b >= 0 && b < _pcsList.Count)
                _pcsList[b].RefreshBlackStartBusContext(busV);
        }

        private void RefreshAllUnitBlackStartBusContexts()
        {
            int unitCount = (_pcsList.Count + 1) / 2;
            for (int u = 0; u < unitCount; u++)
                RefreshUnitBlackStartBusContext(u);
        }

        /// <summary>PCS.Update 之后同步单元变与站用电分摊（见 <see cref="UnitTransformerIslandSync"/>）。</summary>
        private void SyncUnitTransformerAfterPcsUpdate(DateTime simTime, TimeSpan simStep) =>
            UnitTransformerIslandSync.SyncAfterPcsUpdate(
                IsMainBreakerClosed,
                StationBus35LineVoltageV,
                _unitTransformers,
                _mainTransformer,
                _pcsList,
                IsUnitBreakerClosed,
                _pcsCfg,
                simTime,
                simStep);

        // 更新系统状态
        private void Update(DateTime simTime, TimeSpan elapsed, TimeSpan integrationElapsed)
        {
            int n = Math.Min(_bmsRackDevices.Count, _pcsList.Count);
            for (int i = 0; i < n; i++)
            {
                var bms = _bmsRackDevices[i];

                if (bms.IsLinked)
                {
                    var rackState = bms.Rack.GetRackState();
                    if (rackState == null) continue;

                    _pcsList[i].Update(rackState.TotalVoltage, rackState.IsFault, simTime, elapsed, integrationElapsed);
                    bms.UpdatePhysics(-_pcsList[i].GetCurrentState().DcCurrent, 25.0, simTime, integrationElapsed);
                }
                else
                {
                    _pcsList[i].Update(0, 0, simTime, elapsed, integrationElapsed);
                    bms.UpdatePhysics(0, 25.0, simTime, integrationElapsed);
                }
            }
        }

        // // 示例使用
        // public void EssMain(string[] args)
        // {
        //     if (args[0] != null)
        //     {
        //         modelName = args[0];
        //     }   
        // }

        //private static void PrintSystemState(EnergyStorageSystem ess)
        //{
        //    DateTime dt = DateTime.Now;
        //    var pcsState = ess.GetPcsState();
        //    var rackState = ess.GetBatteryRackState();
        //    Console.WriteLine($"{modelName}\t{dt.ToString()}" + $"ActivePower:{pcsState.ActivePower:F1}kW\tMinClusterSOC:{rackState.MinClusterSOC * 100:F1}%\t" );
        //}
    }
}

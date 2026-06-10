using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using log4net;
using Microsoft.Extensions.Hosting;

namespace EssSimulator.EssSimModelApi
{
    /// <summary>
    /// BMS 堆级控制：一键并网 + 黑启动模式。
    /// </summary>
    public class BmsLinkService : BackgroundService
    {
        private const ushort GridStatusIdle = 0;
        private const ushort GridStatusRunning = 1;
        private const ushort GridStatusSuccess = 2;
        private const ushort GridStatusFailed = 3;

        private const ushort BlackStartStatusActive = 3;
        private const ushort BlackStartStatusEnterFailed = 4;
        private const ushort BlackStartStatusExited = 5;

        private readonly ILog _log = LogManager.GetLogger(typeof(BmsLinkService));
        private readonly Dictionary<int, ushort> _lastBlackStartCommand = new();

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var store = SimulatorHost.Instance;
            EnergyStorageSystem? ess = null;

            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                ess ??= store.Get<EnergyStorageSystem>("ess");
                if (ess == null) continue;

                int count = Math.Min(ess._batteryRacks.Count, ess._pcsList.Count);
                for (int i = 0; i < count; i++)
                {
                    var bmsData = store.Get<BatteryManagementSystemData>($"bms{i + 1}");
                    if (bmsData?.BatteryStacks == null || bmsData.BatteryStacks.Count == 0)
                        continue;

                    try
                    {
                        var modbus = store.Get<ModbusSimServer>($"simBms{i + 1}");
                        var stack = bmsData.BatteryStacks[0];
                        var rack = ess._batteryRacks[i];
                        ApplyLinkLogic(i, stack, rack, modbus);
                        ApplyBlackStartLogic(i, stack, rack, ess, modbus);
                    }
                    catch
                    {
                        // 单路失败不影响其它 BMS
                    }
                }
            }
        }

        private void ApplyLinkLogic(
            int bmsIndex,
            BatteryStack stack,
            BatteryRackSimulator rack,
            ModbusSimServer? modbus)
        {
            var rackState = rack.GetRackState();
            if (rackState == null) return;

            if (stack.IsPcsLinked && stack.BMSFaultSummary > 0)
            {
                SetLinked(stack, rackState, linked: false);
                stack.GridConnectStatus = GridStatusFailed;
                _log.Info(
                    $"[BmsLink] auto-disconnect bms{bmsIndex + 1}, 三级报警汇总={stack.BMSFaultSummary}, status=3");
            }

            if (stack.GridConnectCommand == 0)
                return;

            if (stack.GridConnectStatus == GridStatusRunning || stack.GridConnectStatus == GridStatusSuccess)
            {
                ClearConnectCommandPulse(modbus, stack);
                return;
            }

            stack.GridConnectStatus = GridStatusRunning;
            ClearConnectCommandPulse(modbus, stack);
            TryExecuteGridConnect(bmsIndex, stack, rack);
        }

        private void ApplyBlackStartLogic(
            int bmsIndex,
            BatteryStack stack,
            BatteryRackSimulator rack,
            EnergyStorageSystem ess,
            ModbusSimServer? modbus)
        {
            ushort cmd = stack.BlackStartCommand;
            _lastBlackStartCommand.TryGetValue(bmsIndex, out ushort lastCmd);

            bool enterRequested = cmd == 1 && lastCmd != 1;
            bool exitRequested = cmd == 0 && lastCmd == 1;

            if (enterRequested)
            {
                if (Is220KvBusEnergized(ess))
                {
                    FailBlackStartEnter(bmsIndex, stack, modbus, "220kV母线带电");
                }
                else if (!stack.IsPcsLinked)
                {
                    stack.GridConnectStatus = GridStatusRunning;
                    if (!TryExecuteGridConnect(bmsIndex, stack, rack))
                        FailBlackStartEnter(bmsIndex, stack, modbus, "并网失败");
                    else
                        SucceedBlackStartEnter(bmsIndex, stack);
                }
                else
                {
                    SucceedBlackStartEnter(bmsIndex, stack);
                }
            }
            else if (exitRequested)
            {
                stack.BlackStartEnterSuccess = 0;
                stack.BlackStartStatus = BlackStartStatusExited;
                _log.Info($"[BmsBlackStart] exit bms{bmsIndex + 1}, status=5");
            }

            _lastBlackStartCommand[bmsIndex] = stack.BlackStartCommand;
        }

        /// <summary>尝试 BMS 与 PCS 直流侧并网；已并网时直接返回 true。</summary>
        private bool TryExecuteGridConnect(int bmsIndex, BatteryStack stack, BatteryRackSimulator rack)
        {
            var rackState = rack.GetRackState();
            if (rackState == null) return false;

            if (stack.IsPcsLinked)
                return true;

            if (stack.BMSFaultSummary > 0)
            {
                SetLinked(stack, rackState, linked: false);
                stack.GridConnectStatus = GridStatusFailed;
                _log.Info(
                    $"[BmsLink] connect failed bms{bmsIndex + 1}, 三级报警汇总={stack.BMSFaultSummary}, status=3");
                return false;
            }

            SetLinked(stack, rackState, linked: true);
            stack.GridConnectStatus = GridStatusSuccess;
            _log.Info($"[BmsLink] connect success bms{bmsIndex + 1}, status=2");
            return true;
        }

        private void SucceedBlackStartEnter(int bmsIndex, BatteryStack stack)
        {
            stack.BlackStartStatus = BlackStartStatusActive;
            stack.BlackStartEnterSuccess = 1;
            _log.Info($"[BmsBlackStart] enter success bms{bmsIndex + 1}, status=3");
        }

        private void FailBlackStartEnter(int bmsIndex, BatteryStack stack, ModbusSimServer? modbus, string reason)
        {
            stack.BlackStartStatus = BlackStartStatusEnterFailed;
            stack.BlackStartEnterSuccess = 0;
            ClearBlackStartCommand(modbus, stack);
            _log.Info($"[BmsBlackStart] enter failed bms{bmsIndex + 1}, {reason}, status=4");
        }

        private static bool Is220KvBusEnergized(EnergyStorageSystem ess) =>
            ess._breaker.IsClosed;

        private static void ClearConnectCommandPulse(ModbusSimServer? modbus, BatteryStack stack)
        {
            stack.GridConnectCommand = 0;
            modbus?.PublishControlToSlave("param11", 0);
        }

        private static void ClearBlackStartCommand(ModbusSimServer? modbus, BatteryStack stack)
        {
            stack.BlackStartCommand = 0;
            modbus?.PublishControlToSlave("param12", 0);
        }

        private static void SetLinked(BatteryStack stack, RackState rackState, bool linked)
        {
            stack.IsPcsLinked = linked;
            rackState.IsPcsLinked = linked;
        }
    }
}

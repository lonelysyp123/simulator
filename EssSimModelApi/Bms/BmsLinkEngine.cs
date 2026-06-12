using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using log4net;

namespace EssSimulator.EssSimModelApi.Bms
{
    /// <summary>BMS 并网脉冲与黑启动状态机（供 BmsLinkService 与 DataExchange ControlEffect 共用）。</summary>
    public static class BmsLinkEngine
    {
        private const ushort GridStatusIdle = 0;
        private const ushort GridStatusRunning = 1;
        private const ushort GridStatusSuccess = 2;
        private const ushort GridStatusFailed = 3;

        private const ushort BlackStartStatusActive = 3;
        private const ushort BlackStartStatusEnterFailed = 4;
        private const ushort BlackStartStatusExited = 5;

        private static readonly ILog Log = LogManager.GetLogger(typeof(BmsLinkEngine));
        private static readonly Dictionary<int, ushort> LastBlackStartCommand = new();

        public static void ApplyForChannel(int bmsIndex)
        {
            var store = SimulatorHost.Instance;
            var ess = store.Get<EnergyStorageSystem>("ess");
            var bmsData = store.Get<BatteryManagementSystemData>($"bms{bmsIndex + 1}");
            if (ess == null || bmsData?.BatteryStacks == null || bmsData.BatteryStacks.Count == 0)
                return;
            if (bmsIndex >= ess._bmsRackDevices.Count)
                return;

            var stack = bmsData.BatteryStacks[0];
            var bms = ess._bmsRackDevices[bmsIndex];
            ApplyLinkLogic(bmsIndex, stack, bms, ess);
            ApplyBlackStartLogic(bmsIndex, stack, bms, ess);
        }

        public static void ApplyAllChannels()
        {
            var ess = SimulatorHost.Instance.Get<EnergyStorageSystem>("ess");
            if (ess == null) return;

            int count = Math.Min(ess._bmsRackDevices.Count, ess._pcsList.Count);
            for (int i = 0; i < count; i++)
                ApplyForChannel(i);
        }

        private static void ApplyLinkLogic(int bmsIndex, BatteryStack stack, BmsRackDevice bms, EnergyStorageSystem ess)
        {
            if (stack.IsPcsLinked && stack.BMSFaultSummary > 0)
            {
                SetLinked(ess, bmsIndex, stack, linked: false);
                stack.GridConnectStatus = GridStatusFailed;
                Log.Info(
                    $"[BmsLink] auto-disconnect bms{bmsIndex + 1}, 三级报警汇总={stack.BMSFaultSummary}, status=3");
            }

            if (stack.GridConnectCommand == 0)
                return;

            if (stack.GridConnectStatus == GridStatusRunning || stack.GridConnectStatus == GridStatusSuccess)
            {
                ClearConnectCommandPulse(stack);
                return;
            }

            stack.GridConnectStatus = GridStatusRunning;
            ClearConnectCommandPulse(stack);
            TryExecuteGridConnect(bmsIndex, stack, bms, ess);
        }

        private static void ApplyBlackStartLogic(
            int bmsIndex,
            BatteryStack stack,
            BmsRackDevice bms,
            EnergyStorageSystem ess)
        {
            ushort cmd = stack.BlackStartCommand;
            LastBlackStartCommand.TryGetValue(bmsIndex, out ushort lastCmd);

            bool enterRequested = cmd == 1 && lastCmd != 1;
            bool exitRequested = cmd == 0 && lastCmd == 1;

            if (enterRequested)
            {
                if (Is220KvBusEnergized(ess))
                    FailBlackStartEnter(bmsIndex, stack, "220kV母线带电");
                else if (!stack.IsPcsLinked)
                {
                    stack.GridConnectStatus = GridStatusRunning;
                    if (!TryExecuteGridConnect(bmsIndex, stack, bms, ess))
                        FailBlackStartEnter(bmsIndex, stack, "并网失败");
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
                Log.Info($"[BmsBlackStart] exit bms{bmsIndex + 1}, status=5");
            }

            LastBlackStartCommand[bmsIndex] = stack.BlackStartCommand;
        }

        private static bool TryExecuteGridConnect(
            int bmsIndex,
            BatteryStack stack,
            BmsRackDevice bms,
            EnergyStorageSystem ess)
        {
            if (stack.IsPcsLinked || bms.IsLinked)
                return true;

            if (stack.BMSFaultSummary > 0)
            {
                SetLinked(ess, bmsIndex, stack, linked: false);
                stack.GridConnectStatus = GridStatusFailed;
                Log.Info(
                    $"[BmsLink] connect failed bms{bmsIndex + 1}, 三级报警汇总={stack.BMSFaultSummary}, status=3");
                return false;
            }

            SetLinked(ess, bmsIndex, stack, linked: true);
            stack.GridConnectStatus = GridStatusSuccess;
            Log.Info($"[BmsLink] connect success bms{bmsIndex + 1}, status=2");
            return true;
        }

        private static void SucceedBlackStartEnter(int bmsIndex, BatteryStack stack)
        {
            stack.BlackStartStatus = BlackStartStatusActive;
            stack.BlackStartEnterSuccess = 1;
            Log.Info($"[BmsBlackStart] enter success bms{bmsIndex + 1}, status=3");
        }

        private static void FailBlackStartEnter(int bmsIndex, BatteryStack stack, string reason)
        {
            stack.BlackStartStatus = BlackStartStatusEnterFailed;
            stack.BlackStartEnterSuccess = 0;
            ClearBlackStartCommand(stack);
            Log.Info($"[BmsBlackStart] enter failed bms{bmsIndex + 1}, {reason}, status=4");
        }

        private static bool Is220KvBusEnergized(EnergyStorageSystem ess) =>
            ess.IsMainBreakerClosed;

        private static void ClearConnectCommandPulse(BatteryStack stack) =>
            stack.GridConnectCommand = 0;

        private static void ClearBlackStartCommand(BatteryStack stack) =>
            stack.BlackStartCommand = 0;

        private static void SetLinked(EnergyStorageSystem ess, int bmsIndex, BatteryStack stack, bool linked)
        {
            stack.IsPcsLinked = linked;
            ess.SetBmsPcsLinked(bmsIndex, linked);
        }
    }
}

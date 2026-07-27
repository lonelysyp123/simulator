using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using EssSimulator.EssDeviceSimModel.Diagnostics;
using EssSimulator.EssDeviceSimModel.Devices;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;

namespace EssSimulator.EssSimModelApi.Bms
{
    /// <summary>BMS 并网脉冲与黑启动状态机（供 BmsLinkService 与 DataExchange ControlEffect 共用）。</summary>
    public static class BmsLinkEngine
    {
        private const ushort GridStatusIdle = 0;
        private const ushort GridStatusRunning = 1;
        private const ushort GridStatusSuccess = 2;
        private const ushort GridStatusFailed = 3;

        private const ushort GridConnectCmdConnect = 1;
        private const ushort GridConnectCmdDisconnect = 2;

        private const ushort BlackStartStatusActive = 3;
        private const ushort BlackStartStatusEnterFailed = 4;
        private const ushort BlackStartStatusExited = 5;

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

        /// <summary>手动并网/离网（供 esscmd setbmsN power on|off）。</summary>
        public static bool TrySetGridPower(int bmsIndex0, bool connect, out string message)
        {
            message = string.Empty;
            var store = SimulatorHost.Instance;
            var ess = store.Get<EnergyStorageSystem>("ess");
            var bmsData = store.Get<BatteryManagementSystemData>($"bms{bmsIndex0 + 1}");
            if (ess == null)
            {
                message = "找不到 ess 模型，请确认仿真已启动";
                return false;
            }

            if (bmsData?.BatteryStacks == null || bmsData.BatteryStacks.Count == 0)
            {
                message = $"找不到 bms{bmsIndex0 + 1} 数据";
                return false;
            }

            if (bmsIndex0 >= ess._bmsRackDevices.Count)
            {
                message = $"bms{bmsIndex0 + 1} 超出当前配置范围";
                return false;
            }

            var stack = bmsData.BatteryStacks[0];
            var bms = ess._bmsRackDevices[bmsIndex0];
            var label = BmsLabel(bmsIndex0, bms);

            if (connect)
            {
                if (stack.IsPcsLinked && bms.IsLinked)
                {
                    message = "已处于并网状态";
                    return true;
                }

                if (stack.BMSFaultSummary > 0 || bms.HasBlockingFault)
                {
                    message = bms.HasBlockingFault
                        ? $"并网失败：Rack 故障码={bms.FaultCode}，请先清除故障"
                        : $"并网失败：三级报警汇总={stack.BMSFaultSummary}，请先清除故障";
                    return false;
                }

                AssignGridConnectStatus(label, stack, GridStatusIdle, "esscmd 手动并网，准备执行");
                stack.GridConnectCommand = GridConnectCmdConnect;
                ApplyForChannel(bmsIndex0);

                if (!stack.IsPcsLinked || !bms.IsLinked)
                {
                    message = $"并网失败，GridConnectStatus={stack.GridConnectStatus}";
                    return false;
                }

                message = "并网成功";
                return true;
            }

            if (!stack.IsPcsLinked && !bms.IsLinked)
            {
                message = "已处于离网状态";
                return true;
            }

            stack.GridConnectCommand = GridConnectCmdDisconnect;
            ApplyForChannel(bmsIndex0);
            message = "离网成功";
            return true;
        }

        /// <summary>启动时按 DTO 默认并网状态（GridConnectStatus=2）建立 PCS↔BMS 物理链路。</summary>
        public static void ApplyStartupGridLinks(EnergyStorageSystem ess)
        {
            var store = SimulatorHost.Instance;
            int count = Math.Min(ess._bmsRackDevices.Count, ess._pcsList.Count);
            for (int i = 0; i < count; i++)
            {
                var bmsData = store.Get<BatteryManagementSystemData>($"bms{i + 1}");
                if (bmsData?.BatteryStacks == null || bmsData.BatteryStacks.Count == 0)
                    continue;

                var stack = bmsData.BatteryStacks[0];
                if (stack.GridConnectStatus != GridStatusSuccess && !stack.IsPcsLinked)
                    continue;

                if (stack.IsPcsLinked && ess._bmsRackDevices[i].IsLinked)
                    continue;

                var label = BmsLabel(i, ess._bmsRackDevices[i]);
                SetLinked(ess, i, stack, linked: true, label, "仿真启动，恢复默认并网链路");
                AssignGridConnectStatus(label, stack, GridStatusSuccess, "仿真启动，GridConnectStatus 默认=2");
            }
        }

        private static void ApplyLinkLogic(int bmsIndex, BatteryStack stack, BmsRackDevice bms, EnergyStorageSystem ess)
        {
            var label = BmsLabel(bmsIndex, bms);

            // 故障等级处置：三级故障（BMSFaultSummary）或 Rack IsFault → BMS 下电（断开 PCS 链路）
            // 一级保护 / 二级告警仅置位遥测，不自动离网。
            if (stack.IsPcsLinked && (stack.BMSFaultSummary > 0 || bms.HasBlockingFault))
            {
                string reason = stack.BMSFaultSummary > 0
                    ? $"三级故障汇总=0x{stack.BMSFaultSummary:X}，自动下电"
                    : $"Rack 故障码={bms.FaultCode}，自动下电";
                SetLinked(ess, bmsIndex, stack, linked: false, label, reason);
                AssignGridConnectStatus(label, stack, GridStatusFailed, reason);
            }

            ushort cmd = stack.GridConnectCommand;
            if (cmd == 0)
                return;

            ClearConnectCommandPulse(stack);

            if (cmd == GridConnectCmdDisconnect)
            {
                TryExecuteGridDisconnect(bmsIndex, stack, bms, ess, label);
                return;
            }

            if (cmd != GridConnectCmdConnect)
                return;

            if (stack.GridConnectStatus == GridStatusRunning)
                return;

            if (stack.IsPcsLinked && stack.GridConnectStatus == GridStatusSuccess)
                return;

            AssignGridConnectStatus(label, stack, GridStatusRunning, "收到并网脉冲 yt0=1");
            TryExecuteGridConnect(bmsIndex, stack, bms, ess, label);
        }

        private static void ApplyBlackStartLogic(
            int bmsIndex,
            BatteryStack stack,
            BmsRackDevice bms,
            EnergyStorageSystem ess)
        {
            var label = BmsLabel(bmsIndex, bms);
            ushort cmd = stack.BlackStartCommand;
            LastBlackStartCommand.TryGetValue(bmsIndex, out ushort lastCmd);

            bool enterRequested = cmd == 1 && lastCmd != 1;
            bool exitRequested = cmd == 0 && lastCmd == 1;

            if (enterRequested)
            {
                if (Is220KvBusEnergized(ess))
                    FailBlackStartEnter(label, stack, "220kV母线带电");
                else if (!stack.IsPcsLinked)
                {
                    AssignGridConnectStatus(label, stack, GridStatusRunning, "黑启动进入前需先并网");
                    if (!TryExecuteGridConnect(bmsIndex, stack, bms, ess, label))
                        FailBlackStartEnter(label, stack, "黑启动前并网失败");
                    else
                        SucceedBlackStartEnter(label, stack);
                }
                else
                {
                    SucceedBlackStartEnter(label, stack);
                }
            }
            else if (exitRequested)
            {
                stack.BlackStartEnterSuccess = 0;
                AssignBlackStartStatus(label, stack, BlackStartStatusExited, "收到黑启动退出命令");
            }

            LastBlackStartCommand[bmsIndex] = stack.BlackStartCommand;
        }

        private static bool TryExecuteGridConnect(
            int bmsIndex,
            BatteryStack stack,
            BmsRackDevice bms,
            EnergyStorageSystem ess,
            string label)
        {
            if (stack.IsPcsLinked || bms.IsLinked)
                return true;

            if (stack.BMSFaultSummary > 0 || bms.HasBlockingFault)
            {
                string reason = stack.BMSFaultSummary > 0
                    ? $"并网拒绝：三级故障汇总=0x{stack.BMSFaultSummary:X}"
                    : $"并网拒绝：Rack 故障码={bms.FaultCode}";
                SetLinked(ess, bmsIndex, stack, linked: false, label, reason);
                AssignGridConnectStatus(label, stack, GridStatusFailed, reason);
                return false;
            }

            SetLinked(ess, bmsIndex, stack, linked: true, label, "并网脉冲执行成功");
            AssignGridConnectStatus(label, stack, GridStatusSuccess, "PCS↔BMS 直流链路已建立");
            return true;
        }

        private static void TryExecuteGridDisconnect(
            int bmsIndex,
            BatteryStack stack,
            BmsRackDevice bms,
            EnergyStorageSystem ess,
            string label)
        {
            if (!stack.IsPcsLinked && !bms.IsLinked)
            {
                AssignGridConnectStatus(label, stack, GridStatusIdle, "收到离网脉冲，当前已断开");
                return;
            }

            SetLinked(ess, bmsIndex, stack, linked: false, label, "收到离网脉冲 yt0=2");
            AssignGridConnectStatus(label, stack, GridStatusIdle, "PCS↔BMS 直流链路已断开");
        }

        private static void SucceedBlackStartEnter(string label, BatteryStack stack)
        {
            AssignBlackStartStatus(label, stack, BlackStartStatusActive, "黑启动进入成功");
            stack.BlackStartEnterSuccess = 1;
        }

        private static void FailBlackStartEnter(string label, BatteryStack stack, string reason)
        {
            AssignBlackStartStatus(label, stack, BlackStartStatusEnterFailed, reason);
            stack.BlackStartEnterSuccess = 0;
            ClearBlackStartCommand(stack);
        }

        private static bool Is220KvBusEnergized(EnergyStorageSystem ess) =>
            ess.IsMainBreakerClosed;

        private static void ClearConnectCommandPulse(BatteryStack stack) =>
            stack.GridConnectCommand = 0;

        private static void ClearBlackStartCommand(BatteryStack stack) =>
            stack.BlackStartCommand = 0;

        private static void SetLinked(
            EnergyStorageSystem ess,
            int bmsIndex,
            BatteryStack stack,
            bool linked,
            string label,
            string reason)
        {
            bool prev = stack.IsPcsLinked;
            stack.IsPcsLinked = linked;
            ess.SetBmsPcsLinked(bmsIndex, linked);
            SimStateChangeLogger.BmsStateChanged(label, "IsPcsLinked", prev, linked, reason);
        }

        private static void AssignGridConnectStatus(string label, BatteryStack stack, ushort status, string reason)
        {
            ushort prev = stack.GridConnectStatus;
            stack.GridConnectStatus = status;
            SimStateChangeLogger.BmsStateChanged(
                label,
                "GridConnectStatus",
                SimStateChangeLogger.FormatGridConnectStatus(prev),
                SimStateChangeLogger.FormatGridConnectStatus(status),
                reason);
        }

        private static void AssignBlackStartStatus(string label, BatteryStack stack, ushort status, string reason)
        {
            ushort prev = stack.BlackStartStatus;
            stack.BlackStartStatus = status;
            SimStateChangeLogger.BmsStateChanged(
                label,
                "BlackStartStatus",
                SimStateChangeLogger.FormatBlackStartStatus(prev),
                SimStateChangeLogger.FormatBlackStartStatus(status),
                reason);
        }

        private static string BmsLabel(int bmsIndex, BmsRackDevice bms) =>
            string.IsNullOrEmpty(bms.DisplayLabel) ? $"bms{bmsIndex + 1}" : bms.DisplayLabel;
    }
}

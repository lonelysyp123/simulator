using EssSimulator.EssSimModelApi;
using EssSimulator.EssSimModelApi.BatteryManagementSystem;
using log4net;
using log4net.Config;
using log4net.Layout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;


namespace EssSimulator.Display
{  
    public class GuiMain
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(GuiMain));
        // GUI 主入口类：负责控制台界面绘制、用户输入处理以及各子视图的刷新逻辑。
        // 该类以独立线程运行 `GuiThread`，在控制台中绘制菜单并跳转到不同的 Draw* 方法，
        // 每个 Draw* 方法负责在按键空闲时循环刷新对应区域的数据（例如主接线、电池信息、单体信息等）。
        // 为了兼容不同终端，类中使用了 `SafeSetCursorPosition` 和 `WriteFixedLine` 来尽量避免因光标设置失败
        // 引起的异常或输出堆叠（重复打印）。
        private static bool _isRunning = true;
        private static volatile bool _fatalShutdownActive;
        private static int _selectedIndex = 0; // 当前选中项索引
        private static readonly CommandHistory _commandHistory = new();
        //private static string[] _menuItems = { "主电气接线", "电池堆簇信息", "电池单体信息","日志信息","命令输入","连接信息"};
        private static string[] _menuItems = { "主电气接线", "电池堆簇信息", "电池单体信息", "命令输入", "连接信息", "日志信息"};

        public  GuiMain()
        {

            // 构造函数：设置控制台输出编码并启动 GUI 线程。
            // 注意：此处不阻塞主线程，GUI 在独立线程中循环处理界面和用户输入。
            Console.OutputEncoding = System.Text.Encoding.UTF8; // 支持Unicode符号

            // 启动独立 GUI 线程，执行 GuiThread 方法
            Thread guiThread = new Thread(GuiThread);
            guiThread.Start();
        }

        /// <summary>黑启动等严重联锁触发时调用，停止 GUI 导航并配合全屏告警。</summary>
        public static void ActivateFatalShutdown()
        {
            _fatalShutdownActive = true;
            _isRunning = false;
        }

        private static bool PollFatalOrContinue()
        {
            if (FatalSystemAlert.PollFatalUi())
            {
                Thread.Sleep(200);
                return true;
            }
            return false;
        }

        private static bool IsNavigationBlocked =>
            FatalSystemAlert.IsActive || _fatalShutdownActive;

        private const int MainLineUnitsPerSection = 4;

        private static bool TryChangeMainLineSection(ref int sectionIndex, int delta)
        {
            if (delta == 0)
                return false;
            int before = sectionIndex;
            sectionIndex = GuiSimDataAccess.ClampMainLineSectionIndex(sectionIndex + delta, MainLineUnitsPerSection);
            return sectionIndex != before;
        }

        private static bool IsMainLinePageKey(ConsoleKeyInfo key, out int delta)
        {
            delta = key.Key switch
            {
                ConsoleKey.UpArrow or ConsoleKey.PageUp or ConsoleKey.LeftArrow => -1,
                ConsoleKey.DownArrow or ConsoleKey.PageDown or ConsoleKey.RightArrow => 1,
                _ => 0
            };
            return delta != 0;
        }

        private CommandProcessor BuildCommandProcessor()
        {
            var commands = new List<ICommand>
            {
                new EssCommand(),
                new BreakerCommand(),
                new DataPointChangeCommand(),
                new DpcAutoTestCommand(),
            };
            return new CommandProcessor(commands);
        }

        /// <summary>主接线内联命令：执行一条后按任意键返回。</summary>
        private void PromptAndExecuteInlineCommand(CommandProcessor processor)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("主电气接线 - 指令输入（执行后自动返回）");
                Console.WriteLine("可用命令: esscmd / breaker / dpc / dpctest");
                Console.WriteLine("Enter 执行  ↑↓ 历史  ←→ 编辑  Esc 清空");
                var input = ConsoleLineReader.ReadLine("cmd> ", _commandHistory);
                if (!string.IsNullOrWhiteSpace(input))
                    processor.ProcessCommand(input);
                Console.WriteLine("按任意键返回主电气接线...");
                Console.ReadKey(true);
            }
            catch
            {
                // 忽略输入阶段异常，确保界面线程持续运行
            }
        }

        private void DrawInterface()
        {
            // 绘制主界面（菜单）
            // - 清屏并绘制标题
            // - 绘制菜单项并标记当前选中项
            // - 在底部显示简要操作提示
            Console.Clear();
            // 标题（黄色）
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== 储能单元模拟器 ===");
            Console.ResetColor();

            // 动态菜单：将选中项反色显示
            for (int i = 0; i < _menuItems.Length; i++)
            {
                if (i == _selectedIndex)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                Console.WriteLine($"  {_menuItems[i]}  ");
                Console.ResetColor();
            }

            // 状态栏：在底部提示用户快捷操作
            SafeSetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("↑↓: 选择 | Enter: 确认 | ESC: 退出");
        }

        private void HandleInput()
        {
            if (PollFatalOrContinue())
                return;

            // 处理菜单中的用户输入（上下选择、回车进入、ESC 退出）
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    // 向上移动选中索引（不越界）
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.DownArrow:
                    // 向下移动选中索引（不越界）
                    _selectedIndex = Math.Min(_menuItems.Length - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.Enter:
                    // 确认选中项，执行对应命令/视图
                    ExecuteCommand();
                    break;

                case ConsoleKey.Escape:
                    if (IsNavigationBlocked)
                    {
                        PollFatalOrContinue();
                        break;
                    }
                    // 退出程序：设置运行标志并直接退出进程
                    _isRunning = false;
                    Environment.Exit(0);
                    break;
            }
        }

        private void ExecuteCommand()
        {
            if (IsNavigationBlocked)
            {
                PollFatalOrContinue();
                return;
            }

            // 根据当前菜单索引执行对应视图的绘制方法，执行前清屏
            Console.Clear();

            switch (_selectedIndex)
            {
                case 0: // 主接线
                    DrawMainElectiralToggle();
                    break;
                case 1: // 电池堆及簇信息
                    DrawBatteryInfo();
                    break;
                case 2: // 电池单体查询
                    DrawCellInfo();
                    break;
                case 3: // 命令输入
                    DrawCmd();
                    break;
                case 4: // 连接信息
                    DrawClientConnectInfo();
                    break;
                case 5: // 日志信息
                    DrawLog();
                    break;
                case 6: // 退出（备用）
                    _isRunning = false;
                    return;
            }

            // 等待用户按键后返回菜单
            Console.WriteLine("\n按任意键返回...");
            Console.ReadKey();
        }

        private void GuiThread()
        {
            // GUI 主循环（运行于独立线程）
            // 循环步骤：绘制界面 -> 处理一次输入
            while (_isRunning)
            {
                if (PollFatalOrContinue())
                    continue;

                DrawInterface();
                HandleInput();
                // 一次循环无需强制休眠，HandleInput 包含阻塞读取
            }

            if (FatalSystemAlert.IsActive)
                FatalSystemAlert.ForceExitProcess();

            // 程序退出时的清理：清屏并提示
            Console.Clear();
            Console.WriteLine("已退出界面。(ESC)");
            Environment.Exit(0);
        }

        // 安全的 SetCursorPosition：在设置前裁剪坐标，避免在小终端或 buffer 较小时抛出 ArgumentOutOfRangeException
        private void SafeSetCursorPosition(int left, int top)
        {
            try
            {
                int bufW = Console.BufferWidth;
                int bufH = Console.BufferHeight;
                if (bufW <= 0 || bufH <= 0) return;
                if (left < 0) left = 0;
                if (top < 0) top = 0;
                if (left >= bufW) left = bufW - 1;
                if (top >= bufH) top = bufH - 1;
                Console.SetCursorPosition(left, top);
            }
            catch (System.Exception)
            {
                // 忽略所有在设置光标时可能出现的异常（例如在某些受限终端中）
            }
        }

        /// <summary>ASCII 主接线图：三层分块展示（电网 / 35kV / 储能单元）。</summary>
        private void DrawMainLine(int sectionIndex, int unitsPerSection)
        {
            int channelCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));
            int sectionCount = Math.Max(1, (int)Math.Ceiling(unitCount / (double)Math.Max(1, unitsPerSection)));
            sectionIndex = Math.Clamp(sectionIndex, 0, sectionCount - 1);
            int unitStart = sectionIndex * unitsPerSection;
            int unitEndExclusive = Math.Min(unitCount, unitStart + unitsPerSection);
            int channelStart = unitStart * 2;
            int channelEndExclusive = Math.Min(channelCount, unitEndExclusive * 2);
            var snap = GuiElectricalReader.ReadMainLine(unitStart, unitEndExclusive);

            SafeSetCursorPosition(0, 0);
            Console.Write(GuiMainLineRenderer.Render(
                snap, unitStart, unitEndExclusive, channelStart, channelEndExclusive,
                sectionIndex, sectionCount, unitCount, channelCount));
        }

        private void DrawLog()
        {
            // 日志视图：通过 LogDisplay 管理日志文件展示，按 Enter 开始/停止日志显示，ESC 返回
            LogDisplay.StartLogFileWatcher();
            ILog log = LogManager.GetLogger(typeof(GuiMain));

            Console.WriteLine("Press Enter to start log display, ESC to exit...");

            bool isDisplaying = false;
            while (true)
            {
                if (PollFatalOrContinue())
                    continue;

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        if (IsNavigationBlocked)
                        {
                            PollFatalOrContinue();
                            continue;
                        }
                        // 退出日志视图时停止日志展示（如果正在展示）
                        if (isDisplaying)
                        {
                            LogDisplay.Stop();
                            isDisplaying = false;
                        }
                        break;
                    }
                    else if (key.Key == ConsoleKey.Enter && !isDisplaying)
                    {
                        // 启动日志显示（LogDisplay 内部处理尾部跟随等）
                        LogDisplay.Start();
                        isDisplaying = true;
                    }
                }

                // 当未按键时，短暂停顿，避免 CPU 占用过高
                Thread.Sleep(200);
            }
        }

        private void DrawMainElectiralToggle()
        {
            // Tab 切换 Spectre.Console 实时视图 与 原 ASCII 图
            bool useLive = false;
            int sectionIndex = 0;
            var processor = BuildCommandProcessor();
            Console.Clear();
            while (true)
            {
                if (PollFatalOrContinue())
                    continue;

                if (useLive)
                {
                    bool switchRequested = false;
                    bool keepLiveMode = false;
                    bool commandRequested = false;
                    AnsiConsole.Live(new Panel("主电气接线").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                    {
                        while (!Console.KeyAvailable)
                        {
                            if (PollFatalOrContinue())
                                break;

                            int channelCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
                            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));
                            int sectionCount = Math.Max(1, (int)Math.Ceiling(unitCount / (double)MainLineUnitsPerSection));
                            int currentSectionIndex = Math.Clamp(sectionIndex, 0, sectionCount - 1);
                            int liveUnitStart = currentSectionIndex * MainLineUnitsPerSection;
                            int liveUnitEndExclusive = Math.Min(unitCount, liveUnitStart + MainLineUnitsPerSection);
                            int channelStart = liveUnitStart * 2;
                            int channelEndExclusive = Math.Min(channelCount, liveUnitEndExclusive * 2);
                            var snap = GuiElectricalReader.ReadMainLine(liveUnitStart, liveUnitEndExclusive);
                            var time = DateTime.Now.ToLongTimeString();

                            var breakerStatus = GuiStatusFormatters.FormatBreakerState(
                                snap.MainBreakerClosed, snap.MainBreakerTripped);
                            var blackStartSummary = GuiStatusFormatters.BuildBlackStartSwitchSummary(channelStart, channelEndExclusive);
                            var modeLabel = snap.PropagationEnabled ? "径向传播 V-I-φ" : "Legacy";
                            var headerText = new Text(
                                $"电气主接线 {time}  单元 {unitCount} / 通道 {channelCount}\n" +
                                $"区域 {currentSectionIndex + 1}/{sectionCount}（UNIT {liveUnitStart + 1}~{liveUnitEndExclusive}）  求解:{modeLabel}\n" +
                                $"PCC {GuiStatusFormatters.FormatVoltage(snap.PccLineVoltageV)}  35kV母线 {GuiStatusFormatters.FormatVoltage(snap.StationBus35LineVoltageV)}\n" +
                                $"主断[{breakerStatus}]  黑启动[{blackStartSummary}]\n" +
                                "操作: ↑/↓/←/→ 翻页 | Tab 视图 | :/C 命令 | Esc 返回\n");
                            var header = new Panel(headerText).Border(BoxBorder.Rounded);

                            var busTable = new Table().Border(TableBorder.Rounded).Title("传播母线节点");
                            busTable.AddColumn("节点");
                            busTable.AddColumn("V / I / φ");
                            busTable.AddColumn("P / Q / PF");
                            int busRowCount = 0;
                            void AddBusRow(BusNodeSnapshot? bus)
                            {
                                if (bus == null) return;
                                busRowCount++;
                                var p = new AcPhasorSnapshot(bus.Value.LineVoltageV, bus.Value.LineCurrentA, bus.Value.PhaseAngleDeg, bus.Value.FrequencyHz);
                                busTable.AddRow(
                                    bus.Value.BusId,
                                    GuiStatusFormatters.FormatAcPhasor(p),
                                    $"P {p.ActivePowerKw:0.0}  Q {p.ReactivePowerKvar:0.0}  PF {p.PowerFactor:0.000}");
                            }
                            AddBusRow(snap.BusGrid);
                            AddBusRow(snap.Bus35Propagation);
                            foreach (var unit in snap.Units)
                                AddBusRow(unit.Bus690);
                            if (busRowCount == 0)
                                busTable.AddRow("—", "传播引擎未启用或无数据", "—");

                            var acTable = new Table().Border(TableBorder.Rounded).Title("交流设备 (V/I/φ → P/Q)");
                            acTable.AddColumn("设备");
                            acTable.AddColumn("相量");
                            acTable.AddColumn("功率");
                            acTable.AddRow("PCC电表", GuiStatusFormatters.FormatAcPhasor(snap.MeterPrimary),
                                $"P {snap.MeterPrimary.ActivePowerKw:0.0}  Q {snap.MeterPrimary.ReactivePowerKvar:0.0}");
                            acTable.AddRow("主变一次", GuiStatusFormatters.FormatAcPhasorViPhi(snap.MainTransformerPrimary),
                                $"P {snap.MainTransformerPrimary.ActivePowerKw:0.0}  Q {snap.MainTransformerPrimary.ReactivePowerKvar:0.0}");
                            acTable.AddRow("主变二次", GuiStatusFormatters.FormatAcPhasorViPhi(snap.MainTransformerSecondary),
                                $"P {snap.MainTransformerSecondary.ActivePowerKw:0.0}  Q {snap.MainTransformerSecondary.ReactivePowerKvar:0.0}");
                            acTable.AddRow("35kV负载", "—", $"P {snap.LoadActivePowerKw:0.0}  Q {snap.LoadReactivePowerKvar:0.0}");

                            var unitTable = new Table().Border(TableBorder.Rounded).Title("储能单元");
                            unitTable.AddColumn("UNIT");
                            unitTable.AddColumn("单元断/单元变");
                            unitTable.AddColumn("PCS-A");
                            unitTable.AddColumn("PCS-B");
                            unitTable.AddColumn("舱-A");
                            unitTable.AddColumn("舱-B");

                            foreach (var unit in snap.Units)
                            {
                                int u = unit.UnitIndex;
                                int a = u * 2;
                                int b = u * 2 + 1;
                                string xfLine = $"{GuiStatusFormatters.FormatBreakerState(unit.UnitBreakerClosed, unit.UnitBreakerTripped)} | 690V {GuiStatusFormatters.FormatAcPhasorViPhi(unit.UnitTransformerSecondary)}";
                                string pcsA = unit.PcsA != null ? GuiStatusFormatters.FormatPcsAcLine(unit.PcsA.Value) : "-";
                                string pcsB = unit.PcsB != null ? GuiStatusFormatters.FormatPcsAcLine(unit.PcsB.Value) : "-";
                                string bmsA = "-", bmsB = "-";
                                if (a < channelCount)
                                {
                                    double s = 100 * GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{a}]._currentState.MinClusterSOC");
                                    double v = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalVoltage");
                                    double c = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalCurrent");
                                    bmsA = $"舱{a + 1} {s:0.0}%/{v:0.0}/{c:0.0} | {GuiStatusFormatters.FormatGridConnectStatus(a)}";
                                }
                                if (b < channelCount)
                                {
                                    double s = 100 * GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{b}]._currentState.MinClusterSOC");
                                    double v = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalVoltage");
                                    double c = GuiSimDataAccess.SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalCurrent");
                                    bmsB = $"舱{b + 1} {s:0.0}%/{v:0.0}/{c:0.0} | {GuiStatusFormatters.FormatGridConnectStatus(b)}";
                                }
                                unitTable.AddRow($"UNIT {u + 1}", xfLine, pcsA, pcsB, bmsA, bmsB);
                            }

                            var grid = new Grid();
                            grid.AddColumn(new GridColumn().Width(Math.Min(Console.WindowWidth - 2, 100)));
                            grid.AddRow(header);
                            grid.AddRow(busTable);
                            grid.AddRow(acTable);
                            grid.AddRow(unitTable);

                            ctx.UpdateTarget(new Panel(grid).RoundedBorder());
                            Thread.Sleep(100);
                        }
                        // 检查按键以切换或退出
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Tab)
                            {
                                switchRequested = true;
                            }
                            else if (IsMainLinePageKey(key, out int pageDelta))
                            {
                                TryChangeMainLineSection(ref sectionIndex, pageDelta);
                                switchRequested = true;
                                keepLiveMode = true;
                            }
                            else if ((key.Key == ConsoleKey.Oem2 && key.Modifiers == ConsoleModifiers.Shift) ||
                                     key.Key == ConsoleKey.C)
                            {
                                commandRequested = true;
                            }
                            else if (key.Key == ConsoleKey.Escape)
                            {
                                if (IsNavigationBlocked)
                                {
                                    PollFatalOrContinue();
                                }
                                else
                                {
                                    // 退出视图
                                    switchRequested = false;
                                }
                            }
                        }
                    });
                    if (!switchRequested)
                    {
                        if (commandRequested)
                        {
                            PromptAndExecuteInlineCommand(processor);
                            Console.Clear();
                            continue;
                        }
                        else
                        {
                            // ESC 退出
                            break;
                        }
                    }
                    useLive = keepLiveMode;
                    Console.Clear();
                }
                else
                {
                    // ASCII 图模式
                    DrawMainLine(sectionIndex, MainLineUnitsPerSection);
                    Thread.Sleep(200);
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (IsNavigationBlocked)
                        {
                            PollFatalOrContinue();
                            continue;
                        }
                        if (key.Key == ConsoleKey.Tab)
                        {
                            useLive = true;
                            Console.Clear();
                        }
                        else if (IsMainLinePageKey(key, out int pageDelta))
                        {
                            if (TryChangeMainLineSection(ref sectionIndex, pageDelta))
                                Console.Clear();
                        }
                        else if ((key.Key == ConsoleKey.Oem2 && key.Modifiers == ConsoleModifiers.Shift) ||
                                 key.Key == ConsoleKey.C)
                        {
                            PromptAndExecuteInlineCommand(processor);
                            Console.Clear();
                        }
                        else if (key.Key == ConsoleKey.Escape)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void DrawBatteryInfo()
        {
            int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            int bmsId = 0;
            while (true)
            {
                if (PollFatalOrContinue())
                    continue;

                Console.Clear();
                bmsId = Math.Clamp(bmsId, 0, unitCount - 1);

                AnsiConsole.Live(new Panel("电池舱信息").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
                        if (FatalSystemAlert.IsActive)
                            break;

                        string basePath = $"bms{bmsId + 1}.BatteryStacks[0]";

                        double totVolt = 0, totCurr = 0, soc = 0, soh = 0, maxCellV = 0, minCellV = 0;
                        int maxClusterId = 0, maxPackId = 0, maxCellId = 0, minClusterId = 0, minPackId = 0, minCellId = 0;
                        totVolt = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.TotalVoltage"));
                        totCurr = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Current"));
                        soc = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.SOC"));
                        soh = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.SOH"));
                        maxCellV = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.MaxCellVoltage"));
                        minCellV = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.MinCellVoltage"));
                        maxClusterId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MaxCellVoltageClusterId"));
                        maxPackId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MaxCellVoltagePackId"));
                        maxCellId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MaxCellVoltageCellId"));
                        minClusterId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MinCellVoltageClusterId"));
                        minPackId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MinCellVoltagePackId"));
                        minCellId = Convert.ToInt32(SimServer.GetExtIfVariableVal($"{basePath}.MinCellVoltageCellId"));

                        var overview = new Table().Border(TableBorder.Rounded).Title($"电池舱总览 - 舱{bmsId + 1}");
                        overview.AddColumn("属性");
                        overview.AddColumn("数值");
                        overview.AddRow("总电压 (V)", $"{totVolt:0.0}");
                        overview.AddRow("总电流 (A)", $"{totCurr:0.0}");
                        overview.AddRow("SOC (%)", $"{soc:0.0}");
                        overview.AddRow("SOH (%)", $"{soh:0.0}");
                        overview.AddRow("簇内最高单体 (V)", $"{maxCellV:0.000} @ 簇{maxClusterId}/包{maxPackId}/单体{maxCellId}");
                        overview.AddRow("簇内最低单体 (V)", $"{minCellV:0.000} @ 簇{minClusterId}/包{minPackId}/单体{minCellId}");
                        overview.AddRow("并离网状态", GuiStatusFormatters.FormatGridConnectStatus(bmsId));
                        overview.AddRow("黑启动模式", GuiStatusFormatters.FormatBlackStartModeStatus(bmsId));

                        var clusterTable = new Table().Border(TableBorder.Rounded).Title("簇列表");
                        clusterTable.AddColumn("簇Id");
                        clusterTable.AddColumn("总电压(V)");
                        clusterTable.AddColumn("总电流(A)");
                        clusterTable.AddColumn("功率(kW)");
                        clusterTable.AddColumn("SOC(%)");
                        clusterTable.AddColumn("SOH(%)");
                        clusterTable.AddColumn("平均单体(V)");
                        clusterTable.AddColumn("单体最高(V)");
                        clusterTable.AddColumn("单体最低(V)");

                        int cluserCount = GuiSimDataAccess.GetClusterCount();
                        for (int i = 0; i < cluserCount; i++)
                        {
                            double cCurr = 0, cVolt = 0, cSoc = 0, cSoh = 0, cAvg = 0, cMax = 0, cMin = 0;
                            cCurr = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.Current"));
                            cVolt = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.TotalVoltage"));
                            cSoc = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.SOC")) * 100;
                            cSoh = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.SOH"));
                            cAvg = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.AvgCellVoltage"));
                            cMax = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.MaxCellVoltage"));
                            cMin = Convert.ToDouble(SimServer.GetExtIfVariableVal($"{basePath}.Cluseter[{i}].Measurements.MinCellVoltage"));
                            double power = cCurr * cVolt / 1000.0;
                            clusterTable.AddRow(i.ToString(), $"{cVolt:0.00}", $"{cCurr:0.00}", $"{power:0.00}", $"{cSoc:0.00}", $"{cSoh:0.00}", $"{cAvg:0.0000}", $"{cMax:0.0000}", $"{cMin:0.0000}");
                        }

                        var grid = new Grid();
                        grid.AddColumn(new GridColumn().Width(Math.Min(Console.WindowWidth - 2, 120)));
                        grid.AddRow(new Text($"上下箭头切换舱 (当前 舱{bmsId + 1}/{unitCount})，Esc 返回"));
                        grid.AddRow(overview);
                        grid.AddRow(clusterTable);

                        ctx.UpdateTarget(new Panel(grid).RoundedBorder());
                        Thread.Sleep(500);
                    }
                });

                if (!Console.KeyAvailable)
                {
                    continue;
                }

                var key = Console.ReadKey(true);
                if (IsNavigationBlocked)
                {
                    PollFatalOrContinue();
                    continue;
                }
                if (key.Key == ConsoleKey.UpArrow)
                {
                    bmsId = Math.Min(unitCount - 1, bmsId + 1);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    bmsId = Math.Max(0, bmsId - 1);
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }
        }

        private void DrawCellInfo()
        {
            int unitCount = Math.Max(1, GuiSimDataAccess.GetEssUnitCount());
            int batlibId = 0;
            int cluserId = 0;

            while (true)
            {
                if (PollFatalOrContinue())
                    continue;

                Console.Clear();
                batlibId = Math.Clamp(batlibId, 0, unitCount - 1);
                int clusterCount = Math.Max(1, GuiSimDataAccess.GetClusterCount());
                cluserId = Math.Clamp(cluserId, 0, clusterCount - 1);

                AnsiConsole.Live(new Panel("电池单体电压").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
                        if (FatalSystemAlert.IsActive)
                            break;

                        string basePath = $"bms{batlibId + 1}.BatteryStacks[0]";

                        // 每个簇有 4 个电池包，每包 104 节单体，分成两列显示以避免过长
                        var packTables = new List<Table>();
                        const int packCount = 4;
                        const int cellsPerPack = 104;
                        const int halfCells = cellsPerPack / 2; // 52

                        string ReadCell(int cellIdx)
                        {
                            float v = 0;
                            try
                            {
                                v = (float)SimServer.GetExtIfVariableVal(string.Format("{0}.Cluseter[{1}].ClusterCellVoltages.CellVoltages[{2}]", basePath, cluserId, cellIdx));
                            }
                            catch
                            {
                                v = 0;
                            }

                            return $"{cellIdx:D3}:{v:0.000}";
                        }

                        for (int pack = 0; pack < packCount; pack++)
                        {
                            var packTable = new Table().Border(TableBorder.Rounded).Title($"包{pack}");
                            packTable.AddColumn("列1");
                            packTable.AddColumn("列2");

                            for (int row = 0; row < halfCells; row++)
                            {
                                int leftIdx = pack * cellsPerPack + row;
                                int rightIdx = pack * cellsPerPack + halfCells + row;
                                packTable.AddRow(ReadCell(leftIdx), ReadCell(rightIdx));
                            }

                            packTables.Add(packTable);
                        }

                        // 4 个包按4列摆放，每个包内部两列，表头为包号（相当于合并单元格）
                        var packGrid = new Grid();
                        packGrid.AddColumn(new GridColumn().Width(40));
                        packGrid.AddColumn(new GridColumn().Width(40));
                        packGrid.AddColumn(new GridColumn().Width(40));
                        packGrid.AddColumn(new GridColumn().Width(40));
                        packGrid.AddRow(packTables[0], packTables[1], packTables[2], packTables[3]);

                        // 顶部提示与时间戳
                        var header = new Text($"上下箭头切换舱 (当前 舱{batlibId + 1}/{unitCount})，左右箭头切换簇 (当前 簇{cluserId + 1}/{clusterCount})，Esc 返回\n时间: {DateTime.Now:HH:mm:ss}");

                        var grid = new Grid();
                        grid.AddColumn(new GridColumn().Width(Math.Min(Console.WindowWidth - 2, 140)));
                        grid.AddRow(header);
                        grid.AddRow(packGrid);

                        ctx.UpdateTarget(new Panel(grid).RoundedBorder());
                        Thread.Sleep(400);
                    }
                });

                if (!Console.KeyAvailable)
                {
                    continue;
                }

                var key = Console.ReadKey(true);
                if (IsNavigationBlocked)
                {
                    PollFatalOrContinue();
                    continue;
                }
                if (key.Key == ConsoleKey.RightArrow)
                {
                    int cc = Math.Max(1, GuiSimDataAccess.GetClusterCount());
                    cluserId = Math.Min(cc - 1, cluserId + 1);
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    cluserId = Math.Max(0, cluserId - 1);
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    batlibId = Math.Min(unitCount - 1, batlibId + 1);
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    batlibId = Math.Max(0, batlibId - 1);
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }
        }

        //打印连接信息
        private void DrawConnectItem()
        {
            
            Console.Clear();

            while (!Console.KeyAvailable)
            {
                SafeSetCursorPosition(0, 0);
                //打印本机地址和端口信息
                foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // 只显示已启用且不是环回接口的网络接口
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPInterfaceProperties ipProps = netInterface.GetIPProperties();

                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            // 只显示IPv4地址
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                Console.WriteLine($"{netInterface.Name}: {addr.Address}");
                            }
                        }
                    }
                }
                //Console.WriteLine("bms");
                Thread.Sleep(500);
            }
        }

        // 在固定位置覆盖并清空本行剩余字符，避免重复打印造成视觉堆叠。
        // 参数：left/top 用于定位光标，text 是要写入的文本。
        // 实现要点：
        // - 使用 SafeSetCursorPosition 做边界保护，避免抛出 ArgumentOutOfRangeException。
        // - 将文本填充或截断到行宽，保证覆盖掉旧内容，避免残留字符。
        // - 捕获所有异常并忽略，以便在受限终端（例如某些 CI 或远程终端）中仍能稳健运行。
        private void WriteFixedLine(int left, int top, string text)
        {
            try
            {
                SafeSetCursorPosition(left, top);
                int width = Math.Max(10, Console.WindowWidth - left - 1);
                if (text.Length < width)
                {
                    text = text + new string(' ', width - text.Length);
                }
                else if (text.Length > width)
                {
                    text = text.Substring(0, width);
                }
                Console.Write(text);
            }
            catch
            {
                // 忽略终端写入异常，保证界面线程不会因为输出问题崩溃
            }
        }

        // 命令输入视图：提供交互式命令行，用户可以输入命令并由 CommandProcessor 解析执行。
        // 该视图使用一组 ICommand 实现（例如 EssCommand, BreakerCommand），通过 Console.ReadLine 获取输入。
        private void DrawCmd()
        {
            Console.Clear();
            var commands = new List<ICommand>
            {              
                new EssCommand(),
                new BreakerCommand(),
                new DataPointChangeCommand(),
                new DpcAutoTestCommand(),
            };

            var processor = new CommandProcessor(commands);

            Console.WriteLine("Enter 执行  ↑↓ 历史  ←→ 编辑  Esc 清空  exit 返回菜单");
            Console.WriteLine();

            while (true)
            {
                if (PollFatalOrContinue())
                    continue;

                var input = ConsoleLineReader.ReadLine("> ", _commandHistory);
                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(input))
                    processor.ProcessCommand(input);
            }
        }

        // 设备接入信息视图：显示本机网络接口、服务器监听端口以及已连接客户端的状态
        // - 枚举 NetworkInterface 获取 IPv4 地址并输出
        // - 读取 SimServer.serverListenInfo 和 SimServer.clientConnectState 展示监听与连接状态
        private void DrawClientConnectInfo()
        {
            Console.Clear();

            while (!Console.KeyAvailable)
            {
                if (FatalSystemAlert.IsActive)
                    break;

                Console.SetCursorPosition(0, 0);
                // 打印本机地址和端口信息（只显示已启用且非回环接口）
                foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (netInterface.OperationalStatus == OperationalStatus.Up &&
                        netInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        IPInterfaceProperties ipProps = netInterface.GetIPProperties();

                        foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                Console.WriteLine($"{netInterface.Name}: {addr.Address}");
                            }
                        }
                    }
                }

                // 打印服务器监听信息
                if(SimServer.serverListenInfo != null && SimServer.serverListenInfo.Count >= 1)
                {
                    foreach (var item in SimServer.serverListenInfo)
                    {
                        Console.WriteLine($"服务器 : {item.Key} ; {item.Value}");
                    }
                }

                // 打印设备接入信息
                if(SimServer.clientConnectState != null && SimServer.clientConnectState.Count >= 1)
                {
                    foreach (var item in SimServer.clientConnectState)
                    {
                        Console.WriteLine($"客户端：{item.Key} 状态：{item.Value}");
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}

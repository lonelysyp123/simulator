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
        // GUI 主入口类：负责控制台界面绘制、用户输入处理以及各子视图的刷新逻辑。
        // 该类以独立线程运行 `GuiThread`，在控制台中绘制菜单并跳转到不同的 Draw* 方法，
        // 每个 Draw* 方法负责在按键空闲时循环刷新对应区域的数据（例如主接线、电池信息、单体信息等）。
        // 为了兼容不同终端，类中使用了 `SafeSetCursorPosition` 和 `WriteFixedLine` 来尽量避免因光标设置失败
        // 引起的异常或输出堆叠（重复打印）。
        private static bool _isRunning = true;
        private static int _selectedIndex = 0; // 当前选中项索引
        private static string? _lastCommand; // 仅保留上一条指令
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
        private static int GetEssUnitCount()
        {
            try
            {
                var list = SimServer.GetExtIfVariableVal("ess._pcsList");
                return list is System.Collections.ICollection c ? c.Count : 0;
            }
            catch { return 2; }
        }

        /// <summary>从 bms1 数据模型读取簇数量（与 Simulator.ClusterCount 一致），失败时回退 12。</summary>
        private static int GetClusterCount()
        {
            try
            {
                var v = SimServer.GetExtIfVariableVal("bms1.BatteryStacks[0].Cluseter.Count");
                if (v is int i) return Math.Max(1, i);
                if (v is long l) return (int)Math.Max(1, Math.Min(int.MaxValue, l));
                if (v != null && int.TryParse(v.ToString(), out int p)) return Math.Max(1, p);
            }
            catch { /* ignore */ }
            return 12;
        }

        private static double SafeGetDouble(string path, double fallback = 0)
        {
            try
            {
                var o = SimServer.GetExtIfVariableVal(path);
                if (o == null) return fallback;
                return Convert.ToDouble(o);
            }
            catch { return fallback; }
        }

        private static bool SafeGetBool(string path, bool fallback = false)
        {
            try
            {
                var o = SimServer.GetExtIfVariableVal(path);
                if (o == null) return fallback;
                return Convert.ToBoolean(o);
            }
            catch { return fallback; }
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

        /// <summary>
        /// 在当前视图内输入并执行一条指令，执行后返回原视图。
        /// </summary>
        private void PromptAndExecuteInlineCommand(CommandProcessor processor)
        {
            try
            {
                Console.Clear();
                Console.WriteLine("主电气接线 - 指令输入（执行后自动返回）");
                Console.WriteLine("可用命令: esscmd / breaker / dpc / dpctest");
                var input = ReadCommandLineWithLastHistory("cmd> ");
                if (!string.IsNullOrWhiteSpace(input))
                {
                    processor.ProcessCommand(input);
                }
                Console.WriteLine("按任意键返回主电气接线...");
                Console.ReadKey(true);
            }
            catch
            {
                // 忽略输入阶段异常，确保界面线程持续运行
            }
        }

        /// <summary>
        /// 读取单行指令，支持“上键回显上一条指令”（仅一条历史，多次上键不重复动作）。
        /// </summary>
        private string ReadCommandLineWithLastHistory(string prompt)
        {
            Console.Write(prompt);
            var buffer = new StringBuilder();
            int renderedLength = 0;
            bool recalledOnce = false;

            void Render()
            {
                string text = buffer.ToString();
                Console.Write('\r');
                Console.Write(prompt);
                Console.Write(text);
                int eraseCount = renderedLength - text.Length;
                if (eraseCount > 0)
                {
                    Console.Write(new string(' ', eraseCount));
                }
                Console.Write('\r');
                Console.Write(prompt);
                Console.Write(text);
                renderedLength = text.Length;
            }

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    var input = buffer.ToString();
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        _lastCommand = input;
                    }
                    return input;
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    if (!recalledOnce && !string.IsNullOrWhiteSpace(_lastCommand))
                    {
                        buffer.Clear();
                        buffer.Append(_lastCommand);
                        recalledOnce = true;
                        Render();
                    }
                    continue;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Remove(buffer.Length - 1, 1);
                        recalledOnce = false;
                        Render();
                    }
                    continue;
                }

                if (!char.IsControl(key.KeyChar))
                {
                    buffer.Append(key.KeyChar);
                    recalledOnce = false;
                    Render();
                }
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
                    // 退出程序：设置运行标志并直接退出进程
                    _isRunning = false;
                    Environment.Exit(0);
                    break;
            }
        }

        private void ExecuteCommand()
        {
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
                DrawInterface();
                HandleInput();
                // 一次循环无需强制休眠，HandleInput 包含阻塞读取
            }

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

        /// <summary>ASCII 主接线图：按“储能单元（每单元2路PCS+2路BMS）”动态分组展示。</summary>
        private void DrawMainLine()
        {
            int channelCount = Math.Max(1, GetEssUnitCount());
            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));
            var time = DateTime.Now.ToLongTimeString();

            bool breakerClosed = SafeGetBool("ess._breaker.IsClosed");
            double primaryVoltage   = SafeGetDouble("ess._mainTransformer._currentState.PrimaryVoltage");
            double secondaryVoltage = SafeGetDouble("ess._mainTransformer._currentState.SecondaryVoltage");
            double primaryCurrent   = SafeGetDouble("ess._mainTransformer._currentState.PrimaryCurrent");
            double secondaryCurrent = SafeGetDouble("ess._mainTransformer._currentState.SecondaryCurrent");
            double loadActivePower   = SafeGetDouble("ess._loadSimulator.ActivePower");
            double loadReactivePower = SafeGetDouble("ess._loadSimulator.ReactivePower");
            double meterIA = SafeGetDouble("em.PhaseACurrent");
            double meterIB = SafeGetDouble("em.PhaseBCurrent");
            double meterIC = SafeGetDouble("em.PhaseCCurrent");
            double meterUab = SafeGetDouble("em.LineVoltageAB");
            double meterUbc = SafeGetDouble("em.LineVoltageBC");
            double meterUca = SafeGetDouble("em.LineVoltageCA");
            double meterActive = SafeGetDouble("em.TotalActivePower");
            double meterReactive = SafeGetDouble("em.TotalReactivePower");

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"电气主接线 [{time}]  （储能单元数: {unitCount}，PCS/BMS通道数: {channelCount}）");
            sb.AppendLine("========= 电压: 220 kV");
            sb.AppendLine("        |");
            sb.AppendLine($"        |   断路器: {(breakerClosed ? "合" : "分")}");
            sb.AppendLine("        |");
            sb.AppendLine($"        |                           电表(一次侧) 相电流 A/B/C: {meterIA:0.0} / {meterIB:0.0} / {meterIC:0.0} A    线电压 AB/BC/CA: {meterUab/1000:0.0} / {meterUbc/1000:0.0} / {meterUca/1000:0.0} kV");
            sb.AppendLine($"        |                                     有功功率: {meterActive:0.0} kW    无功功率: {meterReactive:0.0} kvar");
            sb.AppendLine("        |");
            sb.AppendLine($"        |                           主变: 一次侧 {primaryVoltage / 1000:0.0} kV / {primaryCurrent:0.0} A    二次侧 {secondaryVoltage / 1000:0.0} kV / {secondaryCurrent:0.0} A");
            sb.AppendLine("        |");
            sb.AppendLine($"        |                                                   负载 有功 {loadActivePower:0.0} kW   无功 {loadReactivePower:0.0} kvar");
            sb.AppendLine("        |");
            sb.AppendLine("        |----[断路器]----[电表]------[主变220/35]----[35kV母线]----[负载]");
            sb.AppendLine("        |                                                |");
            sb.AppendLine("        |                                                +--- 并网点 ---+");
            sb.AppendLine("        |                                                                |");

            for (int u = unitCount - 1; u >= 0; u--)
            {
                int a = u * 2;
                int b = u * 2 + 1;

                sb.AppendLine($"        |  ====== [UNIT {u + 1}]  (PCS{a + 1}/PCS{b + 1}  对应  舱{a + 1}/舱{b + 1}) ======");
                // 单元变（35kV/690V）状态（每单元 1 台）
                double uXfPriV = SafeGetDouble($"ess._unitTransformers[{u}]._currentState.PrimaryVoltage");
                double uXfSecV = SafeGetDouble($"ess._unitTransformers[{u}]._currentState.SecondaryVoltage");
                double uXfPriI = SafeGetDouble($"ess._unitTransformers[{u}]._currentState.PrimaryCurrent");
                double uXfSecI = SafeGetDouble($"ess._unitTransformers[{u}]._currentState.SecondaryCurrent");
                sb.AppendLine($"        |   单元变: 一次侧 {uXfPriV/1000:0.0} kV / {uXfPriI:0.0} A   二次侧 {uXfSecV:0.0} V / {uXfSecI:0.0} A");

                if (a < channelCount)
                {
                    double pa = SafeGetDouble($"ess._pcsList[{a}]._currentState.ActivePower");
                    double pr = SafeGetDouble($"ess._pcsList[{a}]._currentState.ReactivePower");
                    double soc = 100 * SafeGetDouble($"ess._batteryRacks[{a}]._currentState.MinClusterSOC");
                    double vdc = SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalVoltage");
                    double idc = SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalCurrent");
                    sb.AppendLine($"        |   PCS{a + 1}: P {pa:0.0} kW  Q {pr:0.0} kvar");
                    sb.AppendLine($"        |   舱{a + 1}:  SOC {soc:0.0}%  Vdc {vdc:0.0} V  Idc {idc:0.0} A");
                }
                sb.AppendLine("        |");
                if (b < channelCount)
                {
                    double pa = SafeGetDouble($"ess._pcsList[{b}]._currentState.ActivePower");
                    double pr = SafeGetDouble($"ess._pcsList[{b}]._currentState.ReactivePower");
                    double soc = 100 * SafeGetDouble($"ess._batteryRacks[{b}]._currentState.MinClusterSOC");
                    double vdc = SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalVoltage");
                    double idc = SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalCurrent");
                    sb.AppendLine($"        |   PCS{b + 1}: P {pa:0.0} kW  Q {pr:0.0} kvar");
                    sb.AppendLine($"        |   舱{b + 1}:  SOC {soc:0.0}%  Vdc {vdc:0.0} V  Idc {idc:0.0} A");
                }

                if (u > 0)
                {
                    sb.AppendLine("        |");
                    sb.AppendLine("        |");
                }
            }
            sb.AppendLine();
            sb.AppendLine("操作: Tab 切换视图 | :/C 输入命令 | Esc 返回");

            SafeSetCursorPosition(0, 0);
            Console.Write(sb.ToString());
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
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
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
            var processor = BuildCommandProcessor();
            Console.Clear();
            while (true)
            {
                if (useLive)
                {
                    bool switchRequested = false;
                    bool commandRequested = false;
                    AnsiConsole.Live(new Panel("主电气接线").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                    {
                        while (!Console.KeyAvailable)
                        {
                            // 采集数据（支持 N 路）
                            bool breakerClosed = SafeGetBool("ess._breaker.IsClosed");
                            var primaryVoltage = SafeGetDouble("ess._mainTransformer._currentState.PrimaryVoltage");
                            var secondaryVoltage = SafeGetDouble("ess._mainTransformer._currentState.SecondaryVoltage");
                            var primaryCurrent = SafeGetDouble("ess._mainTransformer._currentState.PrimaryCurrent");
                            var secondaryCurrent = SafeGetDouble("ess._mainTransformer._currentState.SecondaryCurrent");
                            var loadActivePower = SafeGetDouble("ess._loadSimulator.ActivePower");
                            var loadReactivePower = SafeGetDouble("ess._loadSimulator.ReactivePower");
                            var time = DateTime.Now.ToLongTimeString();
                            int channelCount = Math.Max(1, GetEssUnitCount());
                            int unitCount = Math.Max(1, (int)Math.Ceiling(channelCount / 2.0));

                            // 顶部信息面板（避免 Markup 标签与中文状态混淆，改用 Text）
                            var breakerStatus = breakerClosed ? "合" : "分";
                            var headerText = new Text($"电气主接线 {time}  （单元数 {unitCount}，通道数 {channelCount}）\n状态: 断路器[{breakerStatus}]\n操作: Tab 切换视图 | :/C 输入命令 | Esc 返回\n");
                            var header = new Panel(headerText).Border(BoxBorder.Rounded);

                            // 交流侧表格（一次/二次、负载）
                            var acTable = new Table().Border(TableBorder.Rounded).Title("交流侧");
                            acTable.AddColumn("项目");
                            acTable.AddColumn("电压");
                            acTable.AddColumn("电流");
                            acTable.AddColumn("有功(kW)");
                            acTable.AddColumn("无功(kvar)");
                            acTable.AddRow("一次侧(220kV)", $"{primaryVoltage/1000:0.0} kV", $"{primaryCurrent:0.0} A", "-", "-");
                            acTable.AddRow("二次侧(35kV)", $"{secondaryVoltage/1000:0.0} kV", $"{secondaryCurrent:0.0} A", "-", "-");
                            acTable.AddRow("负载", "-", "-", $"{loadActivePower:0.0}", $"{loadReactivePower:0.0}");

                            // 单元总览表（每行一个 UNIT，包含2路 PCS+2路 BMS）
                            var unitTable = new Table().Border(TableBorder.Rounded).Title("储能单元");
                            unitTable.AddColumn("UNIT");
                            unitTable.AddColumn("PCS-A P/Q");
                            unitTable.AddColumn("PCS-B P/Q");
                            unitTable.AddColumn("舱-A SOC/V/I");
                            unitTable.AddColumn("舱-B SOC/V/I");

                            for (int u = 0; u < unitCount; u++)
                            {
                                int a = u * 2;
                                int b = u * 2 + 1;

                                string pcsA = "-", pcsB = "-", bmsA = "-", bmsB = "-";
                                if (a < channelCount)
                                {
                                    double pa = SafeGetDouble($"ess._pcsList[{a}]._currentState.ActivePower");
                                    double pr = SafeGetDouble($"ess._pcsList[{a}]._currentState.ReactivePower");
                                    pcsA = $"PCS{a + 1} {pa:0.0}/{pr:0.0}";

                                    double s = 100 * SafeGetDouble($"ess._batteryRacks[{a}]._currentState.MinClusterSOC");
                                    double v = SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalVoltage");
                                    double c = SafeGetDouble($"ess._batteryRacks[{a}]._currentState.TotalCurrent");
                                    bmsA = $"舱{a + 1} {s:0.0}%/{v:0.0}/{c:0.0}";
                                }
                                if (b < channelCount)
                                {
                                    double pa = SafeGetDouble($"ess._pcsList[{b}]._currentState.ActivePower");
                                    double pr = SafeGetDouble($"ess._pcsList[{b}]._currentState.ReactivePower");
                                    pcsB = $"PCS{b + 1} {pa:0.0}/{pr:0.0}";

                                    double s = 100 * SafeGetDouble($"ess._batteryRacks[{b}]._currentState.MinClusterSOC");
                                    double v = SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalVoltage");
                                    double c = SafeGetDouble($"ess._batteryRacks[{b}]._currentState.TotalCurrent");
                                    bmsB = $"舱{b + 1} {s:0.0}%/{v:0.0}/{c:0.0}";
                                }

                                unitTable.AddRow($"UNIT {u + 1}", pcsA, pcsB, bmsA, bmsB);
                            }

                            // 电表数据表格（A/B/C 相电流，AB/BC/CA 线电压，总电压/总电流）
                            double meterIA = 0, meterIB = 0, meterIC = 0;
                            double meterUab = 0, meterUbc = 0, meterUca = 0;
                            meterIA = SafeGetDouble("em.PhaseACurrent");
                            meterIB = SafeGetDouble("em.PhaseBCurrent");
                            meterIC = SafeGetDouble("em.PhaseCCurrent");
                            meterUab = SafeGetDouble("em.LineVoltageAB");
                            meterUbc = SafeGetDouble("em.LineVoltageBC");
                            meterUca = SafeGetDouble("em.LineVoltageCA");

                            var meterTable = new Table().Border(TableBorder.Rounded).Title("电表");
                            meterTable.AddColumn("项目");
                            meterTable.AddColumn("数值");
                            meterTable.AddRow("相电流 A/B/C (A)", $"{meterIA:0.0} / {meterIB:0.0} / {meterIC:0.0}");
                            meterTable.AddRow("线电压 AB/BC/CA (V)", $"{meterUab:0.0} / {meterUbc:0.0} / {meterUca:0.0}");

                            // 布局组合
                            var grid = new Grid();
                            grid.AddColumn(new GridColumn().Width(Math.Min(Console.WindowWidth - 2, 80)));
                            grid.AddRow(header);
                            grid.AddRow(acTable);
                            grid.AddRow(meterTable);
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
                            else if ((key.Key == ConsoleKey.Oem2 && key.Modifiers == ConsoleModifiers.Shift) ||
                                     key.Key == ConsoleKey.C)
                            {
                                // ':' 键（通常是 Shift + Oem2），兼容 C 快捷键
                                commandRequested = true;
                            }
                            else if (key.Key == ConsoleKey.Escape)
                            {
                                // 退出视图
                                switchRequested = false;
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
                    useLive = false;
                    Console.Clear();
                }
                else
                {
                    // ASCII 图模式
                    DrawMainLine();
                    Thread.Sleep(200);
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Tab)
                        {
                            useLive = true;
                            Console.Clear();
                        }
                        else if ((key.Key == ConsoleKey.Oem2 && key.Modifiers == ConsoleModifiers.Shift) ||
                                 key.Key == ConsoleKey.C)
                        {
                            // ':' 键（通常是 Shift + Oem2），兼容 C 快捷键
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
            int unitCount = Math.Max(1, GetEssUnitCount());
            int bmsId = 0;
            while (true)
            {
                Console.Clear();
                bmsId = Math.Clamp(bmsId, 0, unitCount - 1);

                AnsiConsole.Live(new Panel("电池舱信息").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
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

                        int cluserCount = GetClusterCount();
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
            int unitCount = Math.Max(1, GetEssUnitCount());
            int batlibId = 0;
            int cluserId = 0;

            while (true)
            {
                Console.Clear();
                batlibId = Math.Clamp(batlibId, 0, unitCount - 1);
                int clusterCount = Math.Max(1, GetClusterCount());
                cluserId = Math.Clamp(cluserId, 0, clusterCount - 1);

                AnsiConsole.Live(new Panel("电池单体电压").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
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
                if (key.Key == ConsoleKey.RightArrow)
                {
                    int cc = Math.Max(1, GetClusterCount());
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

            while (true)
            {
                var input = ReadCommandLineWithLastHistory("> ");
                if (input == "exit")
                {
                    return;
                }
                if(input != null)
                {
                    // 将用户输入传递给命令处理器
                    processor.ProcessCommand(input);
                }
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

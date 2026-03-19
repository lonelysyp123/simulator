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

        private void DrawMainLine()
        {
                        // 绘制主接线信息：从 SimServer（仿真后端）读取多个状态值并格式化输出为 ASCII 图形信息。
                        // 读取操作都通过 SimServer.GetExtIfVariableVal("path") 获取相应的变量值（类型转换由调用方承担）。
                        // 注意：大量直接转换为基本类型（(double)/(bool)）可能在仿真未初始化时抛出异常或导致默认值，
                        // 在必要时可在 SimServer 层做更稳健的空值处理。

                        // 获取断路器、变压器、PCS、直流母线和 SOC 等多个参数并拼接输出
                        bool breakerClosed = (bool)SimServer.GetExtIfVariableVal("ess._breaker.IsClosed");
                        var primaryVoltage = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.PrimaryVoltage");
                        var secondaryVoltage = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.SecondaryVoltage");
                        var primaryCurrent = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.PrimaryCurrent");
                        var secondaryCurrent = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.SecondaryCurrent");
                        var pcs1Reactive = (double)SimServer.GetExtIfVariableVal("ess._pcs1._currentState.ReactivePower");
                        var pcs2Reactive = (double)SimServer.GetExtIfVariableVal("ess._pcs2._currentState.ReactivePower");
                        var pcs1Active = (double)SimServer.GetExtIfVariableVal("ess._pcs1._currentState.ActivePower");
                        var pcs2Active = (double)SimServer.GetExtIfVariableVal("ess._pcs2._currentState.ActivePower");
                        var dcCurrentRack1 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.TotalCurrent");
                        var dcVoltageRack1 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.TotalVoltage");
                        var socRack1 = 100 * (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.MinClusterSOC");
                        var socRack2 = 100 * (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.MinClusterSOC");
                        var dcCurrentRack2 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.TotalCurrent");
                        var dcVoltageRack2 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.TotalVoltage");
                        var loadActivePower = (double)SimServer.GetExtIfVariableVal("ess._loadSimulator.ActivePower");
                        var loadReactivePower = (double)SimServer.GetExtIfVariableVal("ess._loadSimulator.ReactivePower");
                        var meterIA = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseACurrent"));
                        var meterIB = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseBCurrent"));
                        var meterIC = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseCCurrent"));
                        var meterUab = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageAB"));
                        var meterUbc = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageBC"));
                        var meterUca = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageCA"));
                        var switchState = (bool)SimServer.GetExtIfVariableVal("ess._breaker.swState");
                        var time = DateTime.Now.ToLongTimeString();
                        SafeSetCursorPosition(0, 0);
                        Console.WriteLine(@"
电气主接线[{0}]
========= 电压: 10.5 Kv  
                |
                |
                |                                                                三相电流: {19}/{20}/{21} A           有功功率: {17}  kw
                |   状态: {1}   状态: {2}          电压: {3}kV/{5}V           线电压: {22}/{23}/{24} V             无功功率: {18}  kvar
                |----[断路器]---[隔离开关]------------[变压器]-----------------[电表]------------------------------[负载]
                                                  电流: {4}A/{6}A                           |                     
                                                                                            |
                                                                                            | 
                                                                            ---------------------------------
                                                                            |                               |
                                                                            |                               |
                                                                            |                               |
                                                                          [PCS2]                          [PCS1]
                                                                            |  无功功率: {7}   kvar          |   无功功率: {12}   kvar
                                                                            |  有功功率: {8}   kw            |   有功功率: {13}   kw  
                                                                            |                               |
                                                                         [电池舱2]                       [电池舱1]
                                                                        SOC: {9}  %                    SOC: {14}  %
                                                                        直流电压: {10}  V               直流电压: {15}  V
                                                                        直流电流: {11}  A               直流电流: {16}  A



                                                                            
", time,
        breakerClosed.ToString(),          // {1}
        switchState.ToString(),            // {2}
        (primaryVoltage / 1000).ToString("0.0"), // {3}
        primaryCurrent.ToString("00.0"),   // {4}
        secondaryVoltage.ToString("0000.0"), // {5}
        secondaryCurrent.ToString("0000.0"), // {6}
        pcs2Reactive.ToString("0000.0"),     // {7}
        pcs2Active.ToString("0000.0"),       // {8}
        socRack2.ToString("000.0"),         // {9}
        dcVoltageRack2.ToString("0000.0"),   // {10}
        dcCurrentRack2.ToString("0000.0"),   // {11}
        pcs1Reactive.ToString("0000.0"),     // {12}
        pcs1Active.ToString("0000.0"),       // {13}
        socRack1.ToString("000.0"),         // {14}
        dcVoltageRack1.ToString("0000.0"),   // {15}
        dcCurrentRack1.ToString("0000.0"),   // {16}
        loadActivePower.ToString("0000.0"),   // {17}
        loadReactivePower.ToString("0000.0"), // {18}
        meterIA.ToString("0000.0"),           // {19}
        meterIB.ToString("0000.0"),           // {20}
        meterIC.ToString("0000.0"),           // {21}
        meterUab.ToString("0000.0"),         // {22}
        meterUbc.ToString("0000.0"),         // {23}
        meterUca.ToString("0000.0")          // {24}
        );}

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
            Console.Clear();
            while (true)
            {
                if (useLive)
                {
                    bool switchRequested = false;
                    AnsiConsole.Live(new Panel("主电气接线").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                    {
                        while (!Console.KeyAvailable)
                        {
                            // 采集数据
                            bool breakerClosed = (bool)SimServer.GetExtIfVariableVal("ess._breaker.IsClosed");
                            var primaryVoltage = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.PrimaryVoltage");
                            var secondaryVoltage = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.SecondaryVoltage");
                            var primaryCurrent = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.PrimaryCurrent");
                            var secondaryCurrent = (double)SimServer.GetExtIfVariableVal("ess._transformer._currentState.SecondaryCurrent");
                            var pcs1Reactive = (double)SimServer.GetExtIfVariableVal("ess._pcs1._currentState.ReactivePower");
                            var pcs2Reactive = (double)SimServer.GetExtIfVariableVal("ess._pcs2._currentState.ReactivePower");
                            var pcs1Active = (double)SimServer.GetExtIfVariableVal("ess._pcs1._currentState.ActivePower");
                            var pcs2Active = (double)SimServer.GetExtIfVariableVal("ess._pcs2._currentState.ActivePower");
                            var dcCurrentRack1 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.TotalCurrent");
                            var dcVoltageRack1 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.TotalVoltage");
                            var socRack1 = 100 * (double)SimServer.GetExtIfVariableVal("ess._batteryRack._currentState.MinClusterSOC");
                            var socRack2 = 100 * (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.MinClusterSOC");
                            var dcCurrentRack2 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.TotalCurrent");
                            var dcVoltageRack2 = (double)SimServer.GetExtIfVariableVal("ess._batteryRack2._currentState.TotalVoltage");
                            var loadActivePower = (double)SimServer.GetExtIfVariableVal("ess._loadSimulator.ActivePower");
                            var loadReactivePower = (double)SimServer.GetExtIfVariableVal("ess._loadSimulator.ReactivePower");
                            var switchState = (bool)SimServer.GetExtIfVariableVal("ess._breaker.swState");
                            var time = DateTime.Now.ToLongTimeString();

                            // 顶部信息面板（避免 Markup 标签与中文状态混淆，改用 Text）
                            var breakerStatus = breakerClosed ? "合" : "分";
                            var isoStatus = switchState ? "合" : "分";
                            var headerText = new Text($"电气主接线 {time}\n状态: 断路器[{breakerStatus}] 隔离[{isoStatus}]\n");
                            var header = new Panel(headerText).Border(BoxBorder.Rounded);

                            // 交流侧表格（一次/二次、负载）
                            var acTable = new Table().Border(TableBorder.Rounded).Title("交流侧");
                            acTable.AddColumn("项目");
                            acTable.AddColumn("电压");
                            acTable.AddColumn("电流");
                            acTable.AddColumn("有功(kW)");
                            acTable.AddColumn("无功(kvar)");
                            acTable.AddRow("一次侧", $"{primaryVoltage/1000:0.0} kV", $"{primaryCurrent:0.0} A", "-", "-");
                            acTable.AddRow("二次侧", $"{secondaryVoltage:0.0} V", $"{secondaryCurrent:0.0} A", "-", "-");
                            acTable.AddRow("负载", "-", "-", $"{loadActivePower:0.0}", $"{loadReactivePower:0.0}");

                            // PCS 表格
                            var pcsTable = new Table().Border(TableBorder.Rounded).Title("PCS");
                            pcsTable.AddColumn("设备");
                            pcsTable.AddColumn("有功(kW)");
                            pcsTable.AddColumn("无功(kvar)");
                            pcsTable.AddRow("PCS1", $"{pcs1Active:0.0}", $"{pcs1Reactive:0.0}");
                            pcsTable.AddRow("PCS2", $"{pcs2Active:0.0}", $"{pcs2Reactive:0.0}");

                            // 电池舱表格
                            var bmsTable = new Table().Border(TableBorder.Rounded).Title("电池舱");
                            bmsTable.AddColumn("舱");
                            bmsTable.AddColumn("SOC(%)");
                            bmsTable.AddColumn("直流电压(V)");
                            bmsTable.AddColumn("直流电流(A)");
                            bmsTable.AddRow("舱1", $"{socRack1:0.0}", $"{dcVoltageRack1:0.0}", $"{dcCurrentRack1:0.0}");
                            bmsTable.AddRow("舱2", $"{socRack2:0.0}", $"{dcVoltageRack2:0.0}", $"{dcCurrentRack2:0.0}");

                            // 电表数据表格（A/B/C 相电流，AB/BC/CA 线电压，总电压/总电流）
                            double meterIA = 0, meterIB = 0, meterIC = 0;
                            double meterUab = 0, meterUbc = 0, meterUca = 0;
                            meterIA = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseACurrent"));
                            meterIB = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseBCurrent"));
                            meterIC = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.PhaseCCurrent"));
                            meterUab = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageAB"));
                            meterUbc = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageBC"));
                            meterUca = Convert.ToDouble(SimServer.GetExtIfVariableVal("em.LineVoltageCA"));

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
                            grid.AddRow(pcsTable);
                            grid.AddRow(bmsTable);

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
                            else if (key.Key == ConsoleKey.Escape)
                            {
                                // 退出视图
                                switchRequested = false;
                            }
                        }
                    });
                    if (!switchRequested)
                    {
                        // ESC 退出
                        break;
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
            int bmsId = 0;
            while (true)
            {
                Console.Clear();

                AnsiConsole.Live(new Panel("电池舱信息").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
                        string basePath = bmsId == 0 ? "bms1.BatteryStacks[0]" : "bms2.BatteryStacks[0]";

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

                        var overview = new Table().Border(TableBorder.Rounded).Title($"电池舱总览 - 舱{bmsId}");
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

                        int cluserCount = 12;
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
                        grid.AddRow(new Text($"上下箭头切换舱 (当前 {bmsId})，Esc 返回"));
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
                    bmsId = Math.Min(1, bmsId + 1);
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
            int batlibId = 0;
            int cluserId = 0;

            while (true)
            {
                Console.Clear();

                AnsiConsole.Live(new Panel("电池单体电压").RoundedBorder().Border(BoxBorder.Rounded)).Start(ctx =>
                {
                    while (!Console.KeyAvailable)
                    {
                        string basePath = batlibId == 0 ? "bms1.BatteryStacks[0]" : "bms2.BatteryStacks[0]";

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
                        var header = new Text($"上下箭头切换舱 (当前 {batlibId})，左右箭头切换簇 (当前 {cluserId})，Esc 返回\n时间: {DateTime.Now:HH:mm:ss}");

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
                    cluserId = Math.Min(20, cluserId + 1);
                }
                else if (key.Key == ConsoleKey.LeftArrow)
                {
                    cluserId = Math.Max(0, cluserId - 1);
                }
                else if (key.Key == ConsoleKey.UpArrow)
                {
                    batlibId = Math.Min(10, batlibId + 1);
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
            };

            var processor = new CommandProcessor(commands);

            while (true)
            {
                Console.Write("> ");
                var input = Console.ReadLine();
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

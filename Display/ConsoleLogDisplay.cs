using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using log4net;
using log4net.Config;
using Microsoft.VisualBasic;


namespace EssSimulator.Display
{
    public class LogDisplay
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LogDisplay));
        private static readonly int consoleLogHeight = Console.WindowHeight-1; // 控制台日志显示区域高度
        private static string year = DateTime.Now.Year.ToString("0000");
        private static string month = DateTime.Now.Month.ToString("00");
        private static string day = DateTime.Now.Day.ToString("00");

        private static string logFileName = year + "-" + month + "-" + day + ".log"; // 日志文件路径
        private static FileSystemWatcher? _logWatcher;
        private static bool _running;


        public static void Stop()
        {
            _running = false;
        }

        public static void Start()
        {
            _running = true;
            try
            {
                log.Info("[LogDisplay] 日志显示已启动");
            }
            catch { }
        }

        public static void StartLogFileWatcher()
        {
            _logWatcher = new FileSystemWatcher();
            //{
            //    Path = Directory.GetCurrentDirectory()+"//Logs",
            //    Filter = Path.GetFileName(logFilePath),
            //    NotifyFilter = NotifyFilters.LastWrite
            //};

            //string logFilePath = Directory.GetCurrentDirectory();
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            // 统一确保 Logs 目录存在
            var logsDir = isWindows ? @".\Logs" : @"./Logs";
            try
            {
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
            }
            catch { }

            _logWatcher.Path = logsDir;
            _logWatcher.Filter = "*.log";
            _logWatcher.NotifyFilter = NotifyFilters.LastWrite;

            


            _logWatcher.Changed += (sender, e) =>
            {
                try
                {
                    // 读取文件最后几行（文件可能尚未创建，先判断）
                    var target = Path.Combine(logsDir, logFileName);
                    if (!File.Exists(target)) return;
                    var lines = File.ReadAllLines(target);
                    var startIndex = Math.Max(0, lines.Length - consoleLogHeight);
                    var recentLines = lines.Skip(startIndex).Take(consoleLogHeight).ToArray();

                    // 更新控制台显示
                    if(_running)
                    {
                        UpdateConsoleLog(recentLines);
                    }
                }
                catch (IOException ex)
                {
                    // 文件可能正在被写入，忽略短暂错误
                    log.Error("[LogDisplay] 读取日志文件时出错: " + ex.Message);
                    Thread.Sleep(1000);
                }
            };

            _logWatcher.EnableRaisingEvents = true;
        }

        static void UpdateConsoleLog(string[] lines)
        {
            // 保存原始光标位置
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            // 定位到日志区域开始位置（第3行开始）
            Console.SetCursorPosition(0, 0);

            // 清空并重绘日志区域
            for (int i = 0; i < consoleLogHeight; i++)
            {
                string line = i < lines.Length ? lines[i] : "";
                Console.WriteLine(line.PadRight(Console.WindowWidth - 1).Substring(0, Console.WindowWidth - 1));
            }

            // 恢复原始光标位置
            Console.SetCursorPosition(originalLeft, originalTop);
        }
    }
}

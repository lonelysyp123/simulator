using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EssSimulator.Display
{
    public class DpcAutoTestCommand : ICommand
    {
        public string Name => "dpctest";
        public string Description => "执行自动化 DPC 测试";

        public CommandResult Execute(string[] args)
        {
            if (args.Length < 1 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
                return CommandResult.Ok(PrintHelp());

            if (!TryLoadSuite(out var suite, out var loadError))
                return CommandResult.Fail(loadError);

            if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
                return CommandResult.Ok("可用自动化测试:", new { tests = suite.Tests.Select(t => new { t.Name, t.Description }).ToList() });

            var testName = args[0];
            var testCase = suite.Tests.FirstOrDefault(t => t.Name.Equals(testName, StringComparison.OrdinalIgnoreCase));
            if (testCase == null)
                return CommandResult.Fail($"未找到自动化测试: {testName}");

            // 同步执行入口（兼容旧调用）；Web 层应优先调用 RunAsync 以获得进度推送
            var log = new List<string>();
            var result = RunAsync(testCase, stepMsg => log.Add(stepMsg), CancellationToken.None).GetAwaiter().GetResult();
            if (result.Success)
                result = CommandResult.Ok($"测试 [{testCase.Name}] 执行完成", new { testCase = testCase.Name, steps = log });
            else
                result = CommandResult.Fail(result.Message + "\n" + string.Join(Environment.NewLine, log));
            return result;
        }

        /// <summary>异步执行测试用例，逐步通过 progress 回调上报进度（Web 层用于 SignalR 推送）。</summary>
        public async Task<CommandResult> RunAsync(DpcTestCase testCase, Action<string> progress, CancellationToken ct)
        {
            var steps = BuildSteps(testCase);
            if (steps.Count == 0)
                return CommandResult.Fail($"测试 [{testCase.Name}] 没有可执行步骤");

            progress?.Invoke($"开始执行测试 [{testCase.Name}]，步骤数: {steps.Count}");
            if (!string.IsNullOrWhiteSpace(testCase.Description))
                progress?.Invoke($"说明: {testCase.Description}");

            for (int i = 0; i < steps.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var step = steps[i].Trim();
                if (string.IsNullOrWhiteSpace(step))
                    continue;

                progress?.Invoke($"[{i + 1}/{steps.Count}] {step}");

                if (TryParseSleepMilliseconds(step, out var sleepMilliseconds))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(sleepMilliseconds), ct);
                    progress?.Invoke($"等待完成: {sleepMilliseconds} ms");
                    continue;
                }

                if (step.StartsWith("dpc ", StringComparison.OrdinalIgnoreCase))
                {
                    var dpcArgs = step.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                    if (!DataPointChangeCommand.TryExecuteDpcOperation(dpcArgs, out var dpcMessage))
                    {
                        return CommandResult.Fail($"测试 [{testCase.Name}] 执行失败: {dpcMessage}");
                    }

                    progress?.Invoke(dpcMessage);
                    continue;
                }

                return CommandResult.Fail($"测试 [{testCase.Name}] 执行失败: 不支持的步骤 [{step}]，当前仅支持 dpc 和 sleep");
            }

            return CommandResult.Ok($"测试 [{testCase.Name}] 执行完成");
        }

        /// <summary>加载 autotest.json 并返回测试列表（供 Web API 查询）。</summary>
        public static bool TryListTests(out List<DpcTestCase> tests, out string error)
        {
            tests = new List<DpcTestCase>();
            if (!TryLoadSuite(out var suite, out error))
                return false;
            tests = suite.Tests;
            return true;
        }

        private static List<string> BuildSteps(DpcTestCase testCase)
        {
            if (testCase.Steps.Count > 0)
                return testCase.Steps.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();

            if (!string.IsNullOrWhiteSpace(testCase.Script))
            {
                return testCase.Script
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            return new List<string>();
        }

        private static bool TryParseSleepMilliseconds(string step, out int milliseconds)
        {
            milliseconds = 0;
            if (step.StartsWith("sleep(", StringComparison.OrdinalIgnoreCase) && step.EndsWith(")"))
            {
                var body = step["sleep(".Length..^1].Trim();
                return int.TryParse(body, out milliseconds) && milliseconds >= 0;
            }

            if (step.StartsWith("sleep ", StringComparison.OrdinalIgnoreCase))
            {
                var body = step["sleep ".Length..].Trim();
                return int.TryParse(body, out milliseconds) && milliseconds >= 0;
            }

            return false;
        }

        private static bool TryLoadSuite(out DpcTestSuite suite, out string error)
        {
            suite = new DpcTestSuite();
            error = string.Empty;

            var candidatePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "autotest.json"),
                Path.Combine(AppContext.BaseDirectory, "autotest.json")
            };

            var configPath = candidatePaths.FirstOrDefault(File.Exists);
            if (string.IsNullOrWhiteSpace(configPath))
            {
                error = "未找到 autotest.json，请在程序目录或启动目录下创建该文件";
                return false;
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                suite = JsonSerializer.Deserialize<DpcTestSuite>(json, options) ?? new DpcTestSuite();
                return true;
            }
            catch (Exception ex)
            {
                error = $"读取 autotest.json 失败: {ex.Message}";
                return false;
            }
        }

        public static string PrintHelp()
        {
            return new[]
            {
                "用法: dpctest <testName>",
                "  dpctest list         列出 autotest.json 中的测试名称",
                "  dpctest <testName>   执行指定测试",
                "  dpctest help         查看帮助",
                "步骤语法支持:",
                "  dpc <device.point> set <value>",
                "  dpc <device.point> get",
                "  sleep(100) 或 sleep 100   // 单位：毫秒（ms）"
            }.JoinLines();
        }
    }
}

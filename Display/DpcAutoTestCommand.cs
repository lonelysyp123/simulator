using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace EssSimulator.Display
{
public class DpcAutoTestCommand() : ICommand
{
    public string Name => "dpctest";
    public string Description => "执行自动化 DPC 测试";

    public void Execute(string[] args)
    {
        if (args.Length < 1 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return;
        }

        if (!TryLoadSuite(out var suite, out var loadError))
        {
            Console.WriteLine(loadError);
            return;
        }

        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            PrintTestList(suite);
            return;
        }

        var testName = args[0];
        var testCase = suite.Tests.FirstOrDefault(t => t.Name.Equals(testName, StringComparison.OrdinalIgnoreCase));
        if (testCase == null)
        {
            Console.WriteLine($"未找到自动化测试: {testName}");
            return;
        }

        var steps = BuildSteps(testCase);
        if (steps.Count == 0)
        {
            Console.WriteLine($"测试 [{testCase.Name}] 没有可执行步骤");
            return;
        }

        Console.WriteLine($"开始执行测试 [{testCase.Name}]，步骤数: {steps.Count}");
        if (!string.IsNullOrWhiteSpace(testCase.Description))
            Console.WriteLine($"说明: {testCase.Description}");

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i].Trim();
            if (string.IsNullOrWhiteSpace(step))
                continue;

            Console.WriteLine($"[{i + 1}/{steps.Count}] {step}");

            if (TryParseSleepMilliseconds(step, out var sleepMilliseconds))
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(sleepMilliseconds));
                Console.WriteLine($"等待完成: {sleepMilliseconds} ms");
                continue;
            }

            if (step.StartsWith("dpc ", StringComparison.OrdinalIgnoreCase))
            {
                var dpcArgs = step.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1).ToArray();
                if (!DataPointChangeCommand.TryExecuteDpcOperation(dpcArgs, out var dpcMessage))
                {
                    Console.WriteLine($"测试 [{testCase.Name}] 执行失败: {dpcMessage}");
                    return;
                }

                Console.WriteLine(dpcMessage);
                continue;
            }

            Console.WriteLine($"测试 [{testCase.Name}] 执行失败: 不支持的步骤 [{step}]，当前仅支持 dpc 和 sleep");
            return;
        }

        Console.WriteLine($"测试 [{testCase.Name}] 执行完成");
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

        return [];
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

    private static void PrintTestList(DpcTestSuite suite)
    {
        if (suite.Tests.Count == 0)
        {
            Console.WriteLine("autotest.json 中没有定义任何测试");
            return;
        }

        Console.WriteLine("可用自动化测试:");
        foreach (var test in suite.Tests)
        {
            var desc = string.IsNullOrWhiteSpace(test.Description) ? "" : $" - {test.Description}";
            Console.WriteLine($"  {test.Name}{desc}");
        }
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

    private static void PrintHelp()
    {
        Console.WriteLine("用法: dpctest <testName>");
        Console.WriteLine("  dpctest list         列出 autotest.json 中的测试名称");
        Console.WriteLine("  dpctest <testName>   执行指定测试");
        Console.WriteLine("  dpctest help         查看帮助");
        Console.WriteLine("步骤语法支持:");
        Console.WriteLine("  dpc <device.point> set <value>");
        Console.WriteLine("  dpc <device.point> get");
        Console.WriteLine("  sleep(100) 或 sleep 100   // 单位：毫秒（ms）");
    }
}
}

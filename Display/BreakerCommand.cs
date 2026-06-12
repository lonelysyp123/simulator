using EssSimulator.Core;
using EssSimulator.EssDeviceSimModel;
using System;

namespace EssSimulator.Display
{
public class BreakerCommand() : ICommand
{
    public string Name => "breaker";
    public string Description => "breaker 操控命令 (set)";

    public void Execute(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("用法: breaker <operation> <state>");
            return;
        }

        if (!bool.TryParse(args[1], out var flag))
        {
            Console.WriteLine("请输入有效的数字");
            return;
        }

        var objectsCollect = SimulatorHost.Instance;
        EnergyStorageSystem ess = objectsCollect.Get<EnergyStorageSystem>("ess"); ;
        if (ess == null)
        {
            Console.WriteLine("找不到对应的模型");
            return;
        }

        if (args[0] == "set")
        {
            ess.SetMainBreakerClosed(flag);
        }
        else
        {
            Console.WriteLine("操作命令参数不正确");
            return;
        }
        Console.WriteLine($"执行成功");
    }
}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace EssSimulator.Display
{
public class CommandProcessor
{
    private readonly Dictionary<string, ICommand> _commands;

    public CommandProcessor(IEnumerable<ICommand> commands)
    {
        _commands = commands.ToDictionary(c => c.Name.ToLower());
    }

    public void ProcessCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

        if (_commands.TryGetValue(commandName, out var command))
        {
            try
            {
                command.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行命令时出错: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"未知命令: {commandName}");
            Console.WriteLine("当前可用命令: esscmd, breaker, dpc, dpctest");
        }
    }
}
}

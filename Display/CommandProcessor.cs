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

        public IReadOnlyDictionary<string, ICommand> Commands => _commands;

        public CommandResult ProcessCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return CommandResult.Fail("空命令");

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].ToLower();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (_commands.TryGetValue(commandName, out var command))
            {
                try
                {
                    return command.Execute(args);
                }
                catch (Exception ex)
                {
                    return CommandResult.Fail($"执行命令时出错: {ex.Message}");
                }
            }

            return CommandResult.Unknown(commandName);
        }
    }
}

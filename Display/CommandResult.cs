namespace EssSimulator.Display
{
    /// <summary>命令执行结构化结果，供 Web 层序列化为 JSON 返回前端。</summary>
    public sealed class CommandResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = "";
        public string? Command { get; init; }
        public string[]? Args { get; init; }
        public object? Data { get; init; }

        public static CommandResult Ok(string message, object? data = null) => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        public static CommandResult Fail(string message) => new()
        {
            Success = false,
            Message = message
        };

        public static CommandResult Unknown(string command) => new()
        {
            Success = false,
            Message = $"未知命令: {command}，当前可用命令: esscmd, breaker, dpc, dpctest"
        };
    }
}

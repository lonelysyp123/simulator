using EssSimulator.Display;
using Microsoft.AspNetCore.SignalR;

namespace EssSimulator.Web
{
    /// <summary>Web 层命令执行器：复用 CommandProcessor，并支持 dpctest 异步进度推送。</summary>
    public sealed class WebCommandExecutor
    {
        private readonly CommandProcessor _processor;
        private readonly DpcAutoTestCommand _dpctest;
        private readonly IHubContext<RealtimeHub> _hub;

        public WebCommandExecutor(IHubContext<RealtimeHub> hub)
        {
            _hub = hub;
            _dpctest = new DpcAutoTestCommand();
            _processor = new CommandProcessor(new ICommand[]
            {
                new EssCommand(),
                new BreakerCommand(),
                new DataPointChangeCommand(),
                _dpctest
            });
        }

        public IReadOnlyDictionary<string, ICommand> AvailableCommands => _processor.Commands;

        /// <summary>同步执行一条命令（esscmd/breaker/dpc/dpctest list|help）。</summary>
        public CommandResult Execute(string input)
        {
            return _processor.ProcessCommand(input);
        }

        /// <summary>异步执行 dpctest 测试用例，逐步推送进度到 cmdprogress 频道。</summary>
        public async Task<CommandResult> ExecuteDpcTestAsync(string testName, CancellationToken ct)
        {
            if (!DpcAutoTestCommand.TryListTests(out var tests, out var loadError))
                return CommandResult.Fail(loadError);

            var testCase = tests.FirstOrDefault(t => t.Name.Equals(testName, StringComparison.OrdinalIgnoreCase));
            if (testCase == null)
                return CommandResult.Fail($"未找到自动化测试: {testName}");

            return await _dpctest.RunAsync(testCase, PushProgress, ct);
        }

        private void PushProgress(string message)
        {
            try
            {
                _ = _hub.Clients.Group(RealtimeChannels.CommandProgress)
                    .SendAsync(RealtimeMethods.ReceiveCommandProgress, new { testCase = "", message, time = DateTime.Now });
            }
            catch { /* 忽略推送失败 */ }
        }
    }
}

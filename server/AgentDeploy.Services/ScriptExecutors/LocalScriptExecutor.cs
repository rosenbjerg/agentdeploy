using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Scripts;

namespace AgentDeploy.Services.ScriptExecutors
{
    public sealed class LocalScriptExecutor : ILocalScriptExecutor
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IScriptTransformer _scriptTransformer;
        private readonly IProcessExecutionService _processExecutionService;
        
        public LocalScriptExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService)
        {
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            _processExecutionService = processExecutionService;
        }

        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput,
            CancellationToken cancellationToken)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(directory);
            if (_executionOptions.UseWslPathOnWindows)
                scriptFilePath = scriptFilePath.Replace("C:", "/mnt/c");
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);

            var result = await _processExecutionService.Invoke(_executionOptions.Shell, fileArgument, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)), directory);
            if (_executionOptions.UseWslPathOnWindows)
                await Task.Delay(100); // WSL is a bit slow at unlocking the directory, so a delay is needed to avoid an exception
            return result.ExitCode;
        }
    }
}
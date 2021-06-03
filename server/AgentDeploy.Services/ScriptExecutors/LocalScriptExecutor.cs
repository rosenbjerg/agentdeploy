using System;
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

        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(directory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);

            var result = await _processExecutionService.Invoke(_executionOptions.Shell, fileArgument, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)), directory);
            return result.ExitCode;
        }
    }
}
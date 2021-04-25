using System;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Scripts;
using Instances;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.ScriptExecutors
{
    public class LocalScriptExecutor : ILocalScriptExecutor
    {
        private readonly IOperationContext _operationContext;
        private readonly ExecutionOptions _executionOptions;
        private readonly IScriptTransformer _scriptTransformer;
        private readonly ILogger<LocalScriptExecutor> _logger;

        public LocalScriptExecutor(IOperationContext operationContext, ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, ILogger<LocalScriptExecutor> logger)
        {
            _operationContext = operationContext;
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            _logger = logger;
        }

        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(directory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);

            _logger.LogDebug($"Attempting to execute script using shell {_executionOptions.Shell}: {scriptFilePath}");
            
            var instance = new Instance(_executionOptions.Shell, fileArgument);
            instance.DataReceived += (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, _scriptTransformer.HideSecrets(tuple.Data, invocationContext), tuple.Type == DataType.Error));
            _operationContext.OperationCancelled.ThrowIfCancellationRequested();
            return await instance.FinishedRunning();
        }
    }
}
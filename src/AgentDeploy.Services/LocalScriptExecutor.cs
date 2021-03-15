using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Models;
using Instances;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services
{
    public class LocalScriptExecutor : IScriptExecutor
    {
        private readonly IOperationContext _operationContext;
        private readonly ExecutionOptions _executionOptions;
        private readonly ILogger<LocalScriptExecutor> _logger;

        public LocalScriptExecutor(IOperationContext operationContext, ExecutionOptions executionOptions, ILogger<LocalScriptExecutor> logger)
        {
            _operationContext = operationContext;
            _executionOptions = executionOptions;
            _logger = logger;
        }

        public async Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<string, bool> onOutput)
        {
            var scriptFilePath = Path.Combine(directory, "script.sh");
            _logger.LogDebug($"Attempting to execute script using shell {_executionOptions.Shell}: {scriptFilePath}");
            var instance = new Instance(_executionOptions.Shell, scriptFilePath);
            instance.DataReceived += (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error);
            _operationContext.OperationCancelled.ThrowIfCancellationRequested();
            return await instance.FinishedRunning();
        }
    }
}
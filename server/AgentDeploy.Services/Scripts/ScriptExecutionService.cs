using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Websocket;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Scripts
{
    public class ScriptExecutionService : IScriptExecutionService
    {
        private readonly IScriptTransformer _scriptTransformer;
        private readonly IScriptExecutorFactory _scriptExecutorFactory;
        private readonly IConnectionHub _connectionHub;
        private readonly ILogger<ScriptExecutionService> _logger;

        public ScriptExecutionService(IScriptTransformer scriptTransformer, IScriptExecutorFactory scriptExecutorFactory, IConnectionHub connectionHub, ILogger<ScriptExecutionService> logger)
        {
            _scriptTransformer = scriptTransformer;
            _scriptExecutorFactory = scriptExecutorFactory;
            _connectionHub = connectionHub;
            _logger = logger;
        }
        public async Task<ExecutionResult> Execute(ScriptInvocationContext invocationContext, string directory, CancellationToken cancellationToken)
        {
            var scriptLines = await _scriptTransformer.PrepareScriptFile(invocationContext, directory, cancellationToken);
            var processedScriptLines = scriptLines.Select(line => ReplacementUtils.HideSecrets(line, invocationContext)).ToArray();
            var executor = _scriptExecutorFactory.Build(invocationContext);
            _logger.LogInformation($"Executing script using {executor.GetType().Name}");

            var output = new LinkedList<ProcessOutput>();
            var onOutput = await SetupOutputHandlers(invocationContext, processedScriptLines, output, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var exitCode = await executor.Execute(invocationContext, directory, onOutput, cancellationToken);

            var visibleOutput = invocationContext.Script.ShowOutput ? output : Enumerable.Empty<ProcessOutput>();
            var visibleCommand = invocationContext.Script.ShowCommand ? processedScriptLines : Enumerable.Empty<string>();
            return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
        } 

        private async Task<Action<ProcessOutput>> SetupOutputHandlers(ScriptInvocationContext invocationContext, IEnumerable<string> scriptLines, LinkedList<ProcessOutput> output, CancellationToken cancellationToken)
        {
            Action<ProcessOutput>? onOutput = null;
            if (invocationContext.WebSocketSessionId != null && invocationContext.Script.ShowOutput)
            {
                var connection = _connectionHub.PrepareSession(invocationContext.WebSocketSessionId.Value);
                var connected = await connection.AwaitConnection(2, cancellationToken);
                if (connected)
                {
                    onOutput = processOutput => connection.SendOutput(HideSecretsInOutput(processOutput, invocationContext));
                    if (invocationContext.Script.ShowCommand)
                        connection.SendScript(scriptLines);
                }
            }

            onOutput ??= processOutput => output.AddLast(HideSecretsInOutput(processOutput, invocationContext));
            return onOutput;
        }

        private static ProcessOutput HideSecretsInOutput(ProcessOutput processOutput, ScriptInvocationContext scriptInvocationContext)
        {
            var data = ReplacementUtils.HideSecrets(processOutput.Output, scriptInvocationContext);
            return new ProcessOutput(processOutput.Timestamp, data, processOutput.Error);
        }
    }
}
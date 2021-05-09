using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Locking;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Websocket;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptExecutionService : IScriptExecutionService
    {
        private readonly IScriptTransformer _scriptTransformer;
        private readonly IConnectionHub _connectionHub;
        private readonly IScriptInvocationLockService _scriptInvocationLockService;
        private readonly ExecutionOptions _executionOptions;
        private readonly ILogger<ScriptExecutionService> _logger;
        private readonly IOperationContext _operationContext;
        private readonly IScriptExecutorFactory _scriptExecutorFactory;

        public ScriptExecutionService(
            IOperationContext operationContext,
            IScriptExecutorFactory scriptExecutorFactory,
            IScriptTransformer scriptTransformer,
            IConnectionHub connectionHub,
            IScriptInvocationLockService scriptInvocationLockService,
            ExecutionOptions executionOptions,
            ILogger<ScriptExecutionService> logger)
        {
            _scriptTransformer = scriptTransformer;
            _connectionHub = connectionHub;
            _scriptInvocationLockService = scriptInvocationLockService;
            _executionOptions = executionOptions;
            _operationContext = operationContext;
            _scriptExecutorFactory = scriptExecutorFactory;
            _logger = logger;
        }

        public async Task<ExecutionResult> Execute(ScriptInvocationContext invocationContext)
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                using var scriptLock = await _scriptInvocationLockService.Lock(invocationContext.Script, _operationContext.TokenString);
                await DownloadFiles(invocationContext, directory);
                var scriptText = await _scriptTransformer.PrepareScriptFile(invocationContext, directory);

                var executor = _scriptExecutorFactory.Build(invocationContext);
                _logger.LogDebug($"Executing script using {executor.GetType().Name}");

                Action<ProcessOutput>? onOutput = null;
                if (invocationContext.WebSocketSessionId != null && invocationContext.Script.ShowOutput)
                {
                    var connection = _connectionHub.PrepareSession(invocationContext.WebSocketSessionId.Value);
                    var connected = await connection.AwaitConnection(2);
                    if (connected)
                    {
                        onOutput = processOutput => connection.SendOutput(HideSecretsInOutput(processOutput, invocationContext));
                        if (invocationContext.Script.ShowCommand)
                            connection.SendScript(_scriptTransformer.HideSecrets(scriptText, invocationContext));
                    }
                }

                var output = new LinkedList<ProcessOutput>();
                onOutput ??= processOutput => output.AddLast(HideSecretsInOutput(processOutput, invocationContext));
                
                _operationContext.OperationCancelled.ThrowIfCancellationRequested();

                var exitCode = await executor.Execute(invocationContext, directory, onOutput);

                var visibleOutput = invocationContext.Script.ShowOutput ? output : Enumerable.Empty<ProcessOutput>();
                var visibleCommand = invocationContext.Script.ShowCommand ? _scriptTransformer.HideSecrets(scriptText, invocationContext) : string.Empty;
                return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private ProcessOutput HideSecretsInOutput(ProcessOutput processOutput, ScriptInvocationContext scriptInvocationContext)
        {
            var data = _scriptTransformer.HideSecrets(processOutput.Output, scriptInvocationContext);
            return new ProcessOutput(processOutput.Timestamp, data, processOutput.Error);
        }
        
        private async Task DownloadFiles(ScriptInvocationContext invocationContext, string directory)
        {
            _logger.LogDebug($"Downloading input files to {directory}");
            var filesDirectory = $"{directory}{_executionOptions.DirectorySeparatorChar}files";
            Directory.CreateDirectory(filesDirectory);
            foreach (var file in invocationContext.Files)
            {
                var filePath = $"{filesDirectory}{_executionOptions.DirectorySeparatorChar}{file.FileName}";
                await using var outputFile = File.Create(filePath);
                await using var inputStream = file.OpenRead();
                await inputStream.CopyToAsync(outputFile, _operationContext.OperationCancelled);
                invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, filePath, false));
            }
        }

        private string CreateTemporaryDirectory()
        {
            var directory = $"{_executionOptions.TempDir.TrimEnd(_executionOptions.DirectorySeparatorChar)}{_executionOptions.DirectorySeparatorChar}agentd_job_{DateTime.Now:yyyyMMddhhmmssfff}_{Guid.NewGuid()}";
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
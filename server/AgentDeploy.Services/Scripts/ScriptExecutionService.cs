using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
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
        private readonly IScriptExecutionFileService _scriptExecutionFileService;
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
            IScriptExecutionFileService scriptExecutionFileService,
            ExecutionOptions executionOptions,
            ILogger<ScriptExecutionService> logger)
        {
            _scriptTransformer = scriptTransformer;
            _connectionHub = connectionHub;
            _scriptInvocationLockService = scriptInvocationLockService;
            _scriptExecutionFileService = scriptExecutionFileService;
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
                await _scriptExecutionFileService.DownloadFiles(invocationContext, directory);
                var scriptText = await _scriptTransformer.PrepareScriptFile(invocationContext, directory);

                var executor = _scriptExecutorFactory.Build(invocationContext);
                _logger.LogDebug($"Executing script using {executor.GetType().Name}");

                var output = new LinkedList<ProcessOutput>();
                var onOutput = await SetupOutputHandlers(invocationContext, scriptText, output);

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

        private async Task<Action<ProcessOutput>> SetupOutputHandlers(ScriptInvocationContext invocationContext, string scriptText, LinkedList<ProcessOutput> output)
        {
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

            onOutput ??= processOutput => output.AddLast(HideSecretsInOutput(processOutput, invocationContext));
            return onOutput;
        }

        private ProcessOutput HideSecretsInOutput(ProcessOutput processOutput, ScriptInvocationContext scriptInvocationContext)
        {
            var data = _scriptTransformer.HideSecrets(processOutput.Output, scriptInvocationContext);
            return new ProcessOutput(processOutput.Timestamp, data, processOutput.Error);
        }
        

        private string CreateTemporaryDirectory()
        {
            var directory = $"{_executionOptions.TempDir.TrimEnd(_executionOptions.DirectorySeparatorChar)}{_executionOptions.DirectorySeparatorChar}agentd_job_{DateTime.Now:yyyyMMddhhmmssfff}_{Guid.NewGuid()}";
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    public interface IScriptExecutionFileService
    {
        Task DownloadFiles(ScriptInvocationContext invocationContext, string directory);
    }
    public class ScriptExecutionFileService : IScriptExecutionFileService
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IScriptTransformer _scriptTransformer;
        private readonly IOperationContext _operationContext;
        private readonly IProcessExecutionService _processExecutionService;
        private readonly ILogger<ScriptExecutionFileService> _logger;

        public ScriptExecutionFileService(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IOperationContext operationContext, IProcessExecutionService processExecutionService, ILogger<ScriptExecutionFileService> logger)
        {
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            _operationContext = operationContext;
            _processExecutionService = processExecutionService;
            _logger = logger;
        }

        public async Task DownloadFiles(ScriptInvocationContext invocationContext, string directory)
        {
            _logger.LogDebug("Downloading input files to {Directory}", directory);
            var filesDirectory = $"{directory}{_executionOptions.DirectorySeparatorChar}files";
            Directory.CreateDirectory(filesDirectory);
            foreach (var file in invocationContext.Files)
            {
                var filePath = $"{filesDirectory}{_executionOptions.DirectorySeparatorChar}{file.FileName}";
                await using var outputFile = File.Create(filePath);
                await using var inputStream = file.OpenRead();
                await inputStream.CopyToAsync(outputFile, _operationContext.OperationCancelled);
                await ExecuteFilePreprocessing(file, filePath);
                invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, filePath, false));
            }
        }

        private async Task ExecuteFilePreprocessing(AcceptedScriptInvocationFile file, string filePath)
        {
            var preprocessing = file.Preprocessing ?? _executionOptions.DefaultFilePreprocessing;
            if (!string.IsNullOrEmpty(preprocessing))
            {
                _logger.LogDebug("Preprocessing {File} with {Preprocessor}", filePath, preprocessing);
                var preprocess = _scriptTransformer.ReplaceVariables(preprocessing, new Dictionary<string, string>
                {
                    { "FilePath", _scriptTransformer.EscapeWhitespaceInPath(filePath) }
                });
                var preprocessResult = await _processExecutionService.Invoke(_executionOptions.Shell, preprocess, delegate { });
                if (preprocessResult.ExitCode != 0)
                {
                    _logger.LogWarning("Preprocessing of {File} failed with non-zero exit-code {ExitCode}: {Errors}", preprocessResult.ExitCode, preprocessResult.Errors);
                    throw new FilePreprocessingFailedException(file.Name, preprocessResult.ExitCode, preprocessResult.Errors);
                }
            }
        }
    }
}
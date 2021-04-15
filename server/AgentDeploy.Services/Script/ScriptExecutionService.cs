using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Websocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Script
{
    public class ScriptExecutionService
    {
        private readonly ScriptTransformer _scriptTransformer;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionHub _connectionHub;
        private readonly ILogger<ScriptExecutionService> _logger;
        private readonly IOperationContext _operationContext;

        public ScriptExecutionService(
            IOperationContext operationContext,
            IServiceProvider serviceProvider,
            ScriptTransformer scriptTransformer,
            ConnectionHub connectionHub,
            ILogger<ScriptExecutionService> logger)
        {
            _scriptTransformer = scriptTransformer;
            _serviceProvider = serviceProvider;
            _connectionHub = connectionHub;
            _operationContext = operationContext;
            _logger = logger;
        }

        public async Task<ExecutionResult> Execute(ScriptInvocationContext invocationContext)
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                await DownloadFiles(invocationContext, directory);
                var scriptText = await _scriptTransformer.PrepareScriptFile(invocationContext, directory);

                var executor = SelectScriptExecutor(invocationContext);
                _logger.LogDebug($"Executing script using {executor.GetType().Name}");


                Action<ProcessOutput>? onOutput = null;
                if (invocationContext.WebSocketSessionId != null && invocationContext.Script.ShowOutput)
                {
                    var connection = _connectionHub.Prepare(invocationContext.WebSocketSessionId.Value);
                    var connected = await connection.AwaitConnection(2);
                    if (connected)
                    {
                        onOutput = processOutput => connection.SendOutput(processOutput);
                        if (invocationContext.Script.ShowScript)
                            connection.SendScript(_scriptTransformer.HideSecrets(scriptText, invocationContext));
                    }
                }

                
                var output = new LinkedList<ProcessOutput>();
                onOutput ??= processOutput => output.AddLast(processOutput);
                
                _operationContext.OperationCancelled.ThrowIfCancellationRequested();

                var exitCode = await executor.Execute(invocationContext, directory, onOutput);

                var visibleOutput = invocationContext.Script.ShowOutput ? output : Enumerable.Empty<ProcessOutput>();
                var visibleCommand = invocationContext.Script.ShowScript ? _scriptTransformer.HideSecrets(scriptText, invocationContext) : string.Empty;
                return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        private IScriptExecutor SelectScriptExecutor(ScriptInvocationContext invocationContext)
        {
            if (invocationContext.SecureShellOptions == null)
                return _serviceProvider.GetRequiredService<LocalScriptExecutor>();
            
            if (!string.IsNullOrEmpty(invocationContext.SecureShellOptions.Password))
                return _serviceProvider.GetRequiredService<SshPassSecureShellExecutor>();
            
            if (!string.IsNullOrEmpty(invocationContext.SecureShellOptions.PrivateKeyPath))
                return _serviceProvider.GetRequiredService<ExplicitPrivateKeySecureShellExecutor>();
            
            return _serviceProvider.GetRequiredService<ImplicitPrivateKeySecureShellExecutor>();
        }

        private async Task DownloadFiles(ScriptInvocationContext invocationContext, string directory)
        {
            _logger.LogDebug($"Downloading input files to {directory}");
            var filesDirectory = Path.Combine(directory, "files");
            Directory.CreateDirectory(filesDirectory);
            foreach (var file in invocationContext.Files)
            {
                var filePath = Path.Combine(filesDirectory, file.FileName);
                await using var outputFile = File.Create(filePath);
                await using var inputStream = file.OpenRead();
                await inputStream.CopyToAsync(outputFile, _operationContext.OperationCancelled);
                invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, ScriptArgumentType.String, filePath, false));
            }
        }

        private static string CreateTemporaryDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"agentd_job_{DateTime.Now:yyyyMMddhhmmssfff}");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
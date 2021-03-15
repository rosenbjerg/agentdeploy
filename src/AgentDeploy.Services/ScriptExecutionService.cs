using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Models;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services
{
    public class ScriptExecutionService
    {
        private readonly ScriptTransformer _scriptTransformer;
        private readonly SecureShellExecutor _secureShellExecutor;
        private readonly LocalScriptExecutor _localScriptExecutor;
        private readonly ILogger<ScriptExecutionService> _logger;
        private readonly IOperationContext _operationContext;

        public ScriptExecutionService(IOperationContext operationContext, ScriptTransformer scriptTransformer, SecureShellExecutor secureShellExecutor, LocalScriptExecutor localScriptExecutor, ILogger<ScriptExecutionService> logger)
        {
            _scriptTransformer = scriptTransformer;
            _secureShellExecutor = secureShellExecutor;
            _localScriptExecutor = localScriptExecutor;
            _operationContext = operationContext;
            _logger = logger;
        }
        public async Task<ExecutionResult> Execute(ScriptExecutionContext executionContext)
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                await DownloadFiles(executionContext, directory);
                var scriptText = await _scriptTransformer.PrepareScriptFile(executionContext, directory);

                var output = new LinkedList<ProcessOutput>();

                IScriptExecutor executor = executionContext.SecureShellOptions != null ? _secureShellExecutor : _localScriptExecutor;
                _logger.LogDebug($"Executing script using {executor.GetType().Name}");
                
                var exitCode = await executor.Execute(executionContext, directory,
                    (data, error) => output.AddLast(new ProcessOutput(DateTime.UtcNow, _scriptTransformer.HideSecrets(data, executionContext.Arguments), error)));

                var visibleOutput = executionContext.Script.ShowOutput ? output : Enumerable.Empty<ProcessOutput>();
                var visibleCommand = executionContext.Script.ShowCommand ? _scriptTransformer.HideSecrets(scriptText, executionContext.Arguments) : string.Empty;
                return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
        
        private async Task DownloadFiles(ScriptExecutionContext executionContext, string directory)
        {
            _logger.LogDebug($"Downloading input files to {directory}");
            var filesDirectory = Path.Combine(directory, "files");
            Directory.CreateDirectory(filesDirectory);
            foreach (var file in executionContext.Files)
            {
                var filePath = Path.Combine(filesDirectory, file.FileName);
                await using var outputFile = File.Create(filePath);
                await using var inputStream = file.OpenRead();
                await inputStream.CopyToAsync(outputFile, _operationContext.OperationCancelled);
                executionContext.Arguments.Add(new InvocationArgument(file.Name, ArgumentType.String, filePath, false));
            }
        }

        private static string CreateTemporaryDirectory()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"agentdeploy_{DateTime.Now:yyyyMMddhhmmss}");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
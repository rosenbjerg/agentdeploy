using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;

namespace AgentDeploy.Services
{
    public class ScriptExecutionService
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly ScriptTransformer _scriptTransformer;

        public ScriptExecutionService(ExecutionOptions executionOptions, ScriptTransformer scriptTransformer)
        {
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
        }
        public async Task<ExecutionResult> Execute(Script script, ScriptExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var directory = CreateTemporaryDirectory(out string scriptFilePath, out string filesDirectory);
            try
            {
                await DownloadFiles(executionContext, cancellationToken, filesDirectory);
                
                cancellationToken.ThrowIfCancellationRequested();
                var scriptText = await _scriptTransformer.PrepareScriptFile(script, executionContext, scriptFilePath, cancellationToken);

                var output = new LinkedList<ProcessOutput>();

                IScriptExecutor executor = executionContext.SecureShellOptions != null
                    ? new SecureShellExecutor(_executionOptions)
                    : new LocalScriptExecutor(_executionOptions);

                var exitCode = await executor.Execute(executionContext, directory,
                    (data, error) => output.AddLast(new ProcessOutput(DateTime.UtcNow, _scriptTransformer.HideSecrets(data, executionContext.Arguments), error)), 
                    cancellationToken);

                var visibleOutput = script.ShowOutput ? output : Enumerable.Empty<ProcessOutput>();
                var visibleCommand = script.ShowCommand ? _scriptTransformer.HideSecrets(scriptText, executionContext.Arguments) : string.Empty;
                return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
        
        private async Task DownloadFiles(ScriptExecutionContext executionContext, CancellationToken cancellationToken,
            string filesDirectory)
        {
            foreach (var file in executionContext.Files)
            {
                var filePath = Path.Combine(filesDirectory, file.FileName);
                await using var outputFile = File.Create(filePath);
                await using var inputStream = file.OpenRead();
                await inputStream.CopyToAsync(outputFile, cancellationToken);
                var path = _executionOptions.UseWslPath ? WslUtils.TransformPath(filePath) : filePath;
                executionContext.Arguments.Add(new InvocationArgument(file.Name, ArgumentType.String, path, true));
            }
        }

        private static string CreateTemporaryDirectory(out string scriptFilePath, out string filesDirectory)
        {
            var root = Path.GetPathRoot(Path.GetFullPath("./"))!;
            var directory = Path.Combine(root, "agentd", $"deploy_{DateTime.Now:yyyyMMddhhmmss}");
            scriptFilePath = Path.Combine(directory, "script.sh");
            filesDirectory = Path.Combine(directory, "files");
            Directory.CreateDirectory(filesDirectory);
            return directory;
        }
    }
}
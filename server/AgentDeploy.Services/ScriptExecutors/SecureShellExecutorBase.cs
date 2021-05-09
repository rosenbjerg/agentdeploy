using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;

namespace AgentDeploy.Services.ScriptExecutors
{
    public abstract class SecureShellExecutorBase : IScriptExecutor
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IScriptTransformer _scriptTransformer;
        protected readonly IProcessExecutionService ProcessExecutionService;

        protected SecureShellExecutorBase(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService)
        {
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            ProcessExecutionService = processExecutionService;
        }
        
        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput)
        {
            var ssh = invocationContext.SecureShellOptions!;
            void OnUnprocessedOutput(ProcessOutput output) => onOutput(new ProcessOutput(output.Timestamp, _scriptTransformer.HideSecrets(output.Output, invocationContext), output.Error));

            var remoteDirectory = await CopyInternal(directory, ssh, OnUnprocessedOutput);
            if (remoteDirectory == null)
                return -1;
            
            try
            {
                return await ExecuteInternal(ssh, directory, remoteDirectory, OnUnprocessedOutput);
            }
            finally
            {
                await Cleanup(ssh, directory, remoteDirectory, OnUnprocessedOutput);
            }
        }

        protected string StrictHostKeyChecking(SecureShellOptions secureShellOptions)
        {
            return $"-o StrictHostKeyChecking={(secureShellOptions.StrictHostKeyChecking ? "yes" : "no")}";
        }

        protected string Credentials(SecureShellOptions secureShellOptions)
        {
            return $"{secureShellOptions.Username}@{secureShellOptions.Address}";
        }
        
        public abstract Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput);
        public abstract Task<int> Execute(SecureShellOptions ssh, string sourceDirectory, string fileArgument, Action<ProcessOutput> onOutput);
        public abstract Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput);

        protected string GetExecuteCommand(string fileArgument) => $"{_executionOptions.Shell} {fileArgument}";
        protected string GetCleanupCommand(string remoteDirectory) => $"rm -r {_scriptTransformer.EscapeWhitespaceInPath(remoteDirectory, '\'')}";

        private async Task<int> ExecuteInternal(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(remoteDirectory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);
            return await Execute(ssh, sourceDirectory, fileArgument, onOutput);
        }

        private async Task<string?> CopyInternal(string directory, SecureShellOptions ssh, Action<ProcessOutput> onOutput)
        {
            var directoryName = Path.GetFileName(directory);
            var remoteDirectory = $"{ssh.TemporaryAgentDirectory.TrimEnd('/')}/{directoryName}";
            var sourceDirectory = directory;

            onOutput(new ProcessOutput(DateTime.Now, "Copying files to remote..", false));
            var success = await Copy(ssh, sourceDirectory, remoteDirectory, onOutput);
            if (success) onOutput(new ProcessOutput(DateTime.Now, "All files copied to remote", false));
            
            return success ? remoteDirectory : null;
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Script;

namespace AgentDeploy.Services.ScriptExecutors
{
    public abstract class SecureShellExecutorBase : IScriptExecutor
    {
        protected readonly ExecutionOptions ExecutionOptions;
        protected readonly ScriptTransformer ScriptTransformer;

        protected SecureShellExecutorBase(ExecutionOptions executionOptions, ScriptTransformer scriptTransformer)
        {
            ExecutionOptions = executionOptions;
            ScriptTransformer = scriptTransformer;
        }
        
        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput)
        {
            var ssh = invocationContext.SecureShellOptions!;
            void OnUnprocessedOutput(ProcessOutput output) => onOutput(new ProcessOutput(output.Timestamp, ScriptTransformer.HideSecrets(output.Output, invocationContext), output.Error));

            var remoteDirectory = await CopyInternal(directory, ssh, OnUnprocessedOutput);
            if (remoteDirectory == null)
                return -1;
            
            try
            {
                return await ExecuteInternal(ssh, remoteDirectory, OnUnprocessedOutput);
            }
            finally
            {
                await Cleanup(ssh, remoteDirectory, OnUnprocessedOutput);
            }
        }
        
        public abstract Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput);
        public abstract Task<int> Execute(SecureShellOptions ssh, string fileArgument, Action<ProcessOutput> onOutput);
        public abstract Task Cleanup(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput);

        protected string GetExecuteCommand(string fileArgument) => $"{ExecutionOptions.Shell} {fileArgument}";
        protected string GetCleanupCommand(string remoteDirectory) => $"rm -r {remoteDirectory}";

        private async Task<int> ExecuteInternal(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = ScriptTransformer.BuildScriptPath(remoteDirectory);
            var fileArgument = ScriptTransformer.BuildScriptArgument(scriptFilePath);
            return await Execute(ssh, fileArgument, onOutput);
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
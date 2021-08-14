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
        private readonly IScriptTransformer _scriptTransformer;
        protected readonly ExecutionOptions ExecutionOptions;
        protected readonly IProcessExecutionService ProcessExecutionService;

        protected SecureShellExecutorBase(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService)
        {
            ExecutionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            ProcessExecutionService = processExecutionService;
        }
        
        public async Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput)
        {
            var ssh = invocationContext.SecureShellOptions!;
            void OnUnprocessedOutput(ProcessOutput output) => onOutput(new ProcessOutput(output.Timestamp, ReplacementUtils.HideSecrets(output.Output, invocationContext), output.Error));

            var remoteDirectory = await CopyInternal(directory, ssh, OnUnprocessedOutput);
            if (remoteDirectory == null)
                return -1;
            
            try
            {
                return await ExecuteInternal(ssh, remoteDirectory, OnUnprocessedOutput);
            }
            finally
            {
                await Cleanup(ssh, directory, remoteDirectory, OnUnprocessedOutput);
            }
        }

        protected static string StrictHostKeyChecking(SecureShellOptions secureShellOptions)
        {
            var hostKeyChecking = secureShellOptions.HostKeyChecking switch
            {
                HostKeyCheckingOptions.AcceptNew => "accept-new",
                HostKeyCheckingOptions.Yes => "yes",
                HostKeyCheckingOptions.No => "no",
                HostKeyCheckingOptions.Off => "off",
                _ => throw new ArgumentOutOfRangeException(nameof(secureShellOptions), "Unknown StrictHostKeyChecking value")
            };
            return $"-o StrictHostKeyChecking={hostKeyChecking}";
        }

        protected static string Credentials(SecureShellOptions secureShellOptions)
        {
            return $"{secureShellOptions.Username}@{secureShellOptions.Address}";
        }

        protected abstract Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput);
        protected abstract Task<int> Execute(SecureShellOptions ssh, string remoteDirectory,
            string fileArgument, Action<ProcessOutput> onOutput);
        protected abstract Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput);

        protected string GetExecuteCommand(string remoteDirectory, string formattedScriptFileArgument)
        {
            return $"cd {PathUtils.EscapeWhitespaceInPath(remoteDirectory, '\'')} ; {ExecutionOptions.Shell} {formattedScriptFileArgument}";
        }

        protected static string GetCleanupCommand(string remoteDirectory) => $"rm -r {PathUtils.EscapeWhitespaceInPath(remoteDirectory, '\'')}";

        private async Task<int> ExecuteInternal(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(remoteDirectory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);
            return await Execute(ssh, remoteDirectory, fileArgument, onOutput);
        }

        private async Task<string?> CopyInternal(string directory, SecureShellOptions ssh, Action<ProcessOutput> onOutput)
        {
            var directoryName = Path.GetFileName(directory);
            var remoteDirectory = $"{ssh.TemporaryAgentDirectory.TrimEnd('/')}/{directoryName}";
            var sourceDirectory = directory;

            var success = await Copy(ssh, sourceDirectory, remoteDirectory, onOutput);
            return success ? remoteDirectory : null;
        }
    }
}
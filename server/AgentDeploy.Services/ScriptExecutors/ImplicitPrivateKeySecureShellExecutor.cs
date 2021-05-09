using System;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;

namespace AgentDeploy.Services.ScriptExecutors
{
    public sealed class ImplicitPrivateKeySecureShellExecutor : SecureShellExecutorBase, IImplicitPrivateKeySecureShellExecutor
    {
        private readonly IScriptTransformer _scriptTransformer;

        public ImplicitPrivateKeySecureShellExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService) 
            : base(executionOptions, scriptTransformer, processExecutionService)
        {
            _scriptTransformer = scriptTransformer;
        }

        public override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scpCommand = $"-rq {StrictHostKeyChecking(ssh)} -P {ssh.Port} {sourceDirectory} {Credentials(ssh)}:{_scriptTransformer.EscapeWhitespaceInPath(remoteDirectory, '\'')}";
            var result = await ProcessExecutionService.Invoke("scp", scpCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
            return result.ExitCode == 0;
        }

        public override async Task<int> Execute(SecureShellOptions ssh, string sourceDirectory, string fileArgument, Action<ProcessOutput> onOutput)
        {
            var sshCommand = $"-qtt {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetExecuteCommand(fileArgument)}\"";
            var result = await ProcessExecutionService.Invoke("ssh", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
            return result.ExitCode;
        }

        public override async Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var cleanupCommand = $"{StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetCleanupCommand(remoteDirectory)}\"";
            await ProcessExecutionService.Invoke("ssh", cleanupCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
        }
    }
}
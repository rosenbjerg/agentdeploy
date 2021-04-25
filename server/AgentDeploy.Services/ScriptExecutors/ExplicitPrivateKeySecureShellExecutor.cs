using System;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;
using Instances;

namespace AgentDeploy.Services.ScriptExecutors
{
    public class ExplicitPrivateKeySecureShellExecutor : SecureShellExecutorBase, IExplicitPrivateKeySecureShellExecutor
    {
        public ExplicitPrivateKeySecureShellExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer) : base(executionOptions, scriptTransformer)
        {
        }

        public override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scpCommand = $"-rqi {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
            var (exitCode, instance) = await Instance.FinishAsync("scp", scpCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode == 0;
        }

        public override async Task<int> Execute(SecureShellOptions ssh, string fileArgument, Action<ProcessOutput> onOutput)
        {
            var sshCommand = $"-qtti {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetExecuteCommand(fileArgument)}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode;
        }

        public override async Task Cleanup(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var cleanupCommand = $"-o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -i {ssh.PrivateKeyPath} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetCleanupCommand(remoteDirectory)}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", cleanupCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
        }
    }
}
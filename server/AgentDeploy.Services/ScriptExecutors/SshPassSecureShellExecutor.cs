using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;
using Instances;

namespace AgentDeploy.Services.ScriptExecutors
{
    public class SshPassSecureShellExecutor : SecureShellExecutorBase
    {
        public SshPassSecureShellExecutor(ExecutionOptions executionOptions, ScriptTransformer scriptTransformer) 
            : base(executionOptions, scriptTransformer)
        {
        }
        
        public override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var scpCommand = $"-f {passwordFile} scp -rq -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", scpCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode == 0;
            });
        }

        public override async Task<int> Execute(SecureShellOptions ssh, string fileArgument, Action<ProcessOutput> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetExecuteCommand(fileArgument)}\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode;
            });
        }

        public override async Task Cleanup(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetCleanupCommand(remoteDirectory)}\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode;
            });
        }

        private static async Task<T> UsePasswordFile<T>(SecureShellOptions ssh, Func<string, Task<T>> task)
        {
            var passwordFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
            await File.WriteAllTextAsync(passwordFile, ssh.Password);
            try
            {
                return await task(passwordFile);
            }
            finally
            {
                File.Delete(passwordFile);
            }
        }
    }
}
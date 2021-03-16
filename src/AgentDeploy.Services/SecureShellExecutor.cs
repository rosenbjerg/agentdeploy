using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using Instances;

namespace AgentDeploy.Services
{
    public class SecureShellExecutor : IScriptExecutor
    {
        public async Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<string, bool> onOutput)
        {
            var ssh = executionContext.SecureShellOptions!;
            
            var remoteDirectory = await CopyScriptToRemote(directory, ssh, onOutput);
            if (remoteDirectory == null)
                return -1;

            try
            {
                return await Execute(ssh, remoteDirectory, onOutput);
            }
            finally
            {
                await CleanupRemoteDirectory(ssh, remoteDirectory, onOutput);
            }
        }

        private async Task<string?> CopyScriptToRemote(string directory, SecureShellOptions ssh, Action<string, bool> onOutput)
        {
            var directoryName = Path.GetFileName(directory);
            var remoteDirectory = $"{ssh.TemporaryAgentDirectory.TrimEnd('/')}/{directoryName}";
            var sourceDirectory = directory;

            onOutput("Copying files to remote..", false);
            var success = false;
            if (!string.IsNullOrEmpty(ssh.Password))
                success = await CopyUsingSshPass(ssh, sourceDirectory, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                success = await CopyUsingPrivateKey(ssh, sourceDirectory, remoteDirectory, onOutput);
            else
                throw new Exception("Private-key or password must be provided");
            if (success) onOutput("All files copied to remote", false);
            return success ? remoteDirectory : null;
        }

        private async Task<bool> CopyUsingPrivateKey(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<string, bool> onOutput)
        {
            var scpCommand = $"-rqi {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
            var (exitCode, instance) = await Instance.FinishAsync("scp", scpCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            return exitCode == 0;
        }

        private async Task<bool> CopyUsingSshPass(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<string, bool> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var scpCommand = $"-f {passwordFile} scp -rq -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", scpCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
                return exitCode == 0;
            });
        }

        private async Task<int> Execute(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            if (!string.IsNullOrEmpty(ssh.Password))
                return await ExecuteUsingSshPass(ssh, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                return await ExecuteUsingPrivateKey(ssh, remoteDirectory, onOutput);
            else
                throw new Exception();
        }

        private async Task<int> ExecuteUsingPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            var sshCommand = $"-qtti {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", sshCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            return exitCode;
        }

        private async Task<int> ExecuteUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"bash {remoteDirectory}/script.sh\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
                return exitCode;
            });
        }

        private async Task CleanupRemoteDirectory(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            if (!string.IsNullOrEmpty(ssh.Password))
                await CleanupUsingSshPass(ssh, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                await CleanupUsingPrivateKey(ssh, remoteDirectory, onOutput);
            else
                throw new Exception();
        }

        private async Task CleanupUsingPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            var cleanupCommand = $"-o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -i {ssh.PrivateKeyPath} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", cleanupCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            if (exitCode != 0)
            {
                Console.WriteLine(instance);
            }
        }

        private async Task CleanupUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
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
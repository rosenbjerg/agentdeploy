using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Instances;

namespace AgentDeploy.Services
{
    public class SecureShellExecutor : IScriptExecutor
    {
        private readonly ExecutionOptions _executionOptions;

        public SecureShellExecutor(ExecutionOptions executionOptions)
        {
            _executionOptions = executionOptions;
        }

        public async Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<string, bool> onOutput,
            CancellationToken cancellationToken)
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
            var sshCommand = $"ssh -i {ssh.PrivateKeyPath} -t -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
            var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, sshCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            return exitCode;
        }

        private async Task<int> ExecuteUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"sshpass -f {passwordFile} ssh -t -p {ssh.Port} {ssh.Username}@{ssh.Address} \"bash {remoteDirectory}/script.sh\"";
                var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, sshCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
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
            var cleanupCommand = $"ssh -i {ssh.PrivateKeyPath} -t -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
            var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, cleanupCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            if (exitCode != 0)
            {
                Console.WriteLine(instance);
            }
        }

        private async Task CleanupUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<string, bool> onOutput)
        {
            await UsePasswordFile(ssh, async passwordFile =>
            {
                var cleanupCommand = $"sshpass -f {passwordFile} ssh -i {ssh.PrivateKeyPath} -t -p {ssh.Port} {ssh.Username}@{ssh.Address} \"rm -r {remoteDirectory}\"";
                var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, cleanupCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
                return exitCode == 0;
            });
        }

        private async Task<string?> CopyScriptToRemote(string directory, SecureShellOptions ssh, Action<string, bool> onOutput)
        {
            var directoryName = Path.GetFileName(directory);
            var remoteDirectory = $"{ssh.TemporaryAgentDirectory.TrimEnd('/')}/{directoryName}";
            var sourceDirectory = _executionOptions.UseWslPath ? WslUtils.TransformPath(directory) : directory;
            
            if (!string.IsNullOrEmpty(ssh.Password))
                return await CopyUsingSshPass(ssh, sourceDirectory, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                return await CopyUsingPrivateKey(ssh, sourceDirectory, remoteDirectory, onOutput);
            else
                throw new Exception();
        }

        private async Task<string?> CopyUsingPrivateKey(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<string, bool> onOutput)
        {
            var scpCommand = $"scp -r -i {ssh.PrivateKeyPath} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
            var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, scpCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            return exitCode == 0 ? remoteDirectory : null;
        }

        private async Task<string?> CopyUsingSshPass(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<string, bool> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var scpCommand = $"sshpass -f {passwordFile} scp -r -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
                var (exitCode, instance) = await Instance.FinishAsync(_executionOptions.Shell, scpCommand, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
                return exitCode == 0 ? remoteDirectory : null;
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
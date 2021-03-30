using System;
using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Instances;

namespace AgentDeploy.Services
{
    public class SecureShellExecutor : IScriptExecutor
    {
        private readonly ScriptTransformer _scriptTransformer;
        private readonly ExecutionOptions _executionOptions;

        public SecureShellExecutor(ScriptTransformer scriptTransformer, ExecutionOptions executionOptions)
        {
            _scriptTransformer = scriptTransformer;
            _executionOptions = executionOptions;
        }
        
        public async Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<ProcessOutput> onOutput)
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

        private async Task<string?> CopyScriptToRemote(string directory, SecureShellOptions ssh, Action<ProcessOutput> onOutput)
        {
            var directoryName = Path.GetFileName(directory);
            var remoteDirectory = $"{ssh.TemporaryAgentDirectory.TrimEnd('/')}/{directoryName}";
            var sourceDirectory = directory;

            onOutput(new ProcessOutput(DateTime.Now, "Copying files to remote..", false));
            
            var success = false;
            if (!string.IsNullOrEmpty(ssh.Password))
                success = await CopyUsingSshPass(ssh, sourceDirectory, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                success = await CopyUsingPrivateKey(ssh, sourceDirectory, remoteDirectory, onOutput);
            else
                success = await CopyImplicitPrivateKey(ssh, sourceDirectory, remoteDirectory, onOutput);
            
            if (success) onOutput(new ProcessOutput(DateTime.Now, "All files copied to remote", false));
            return success ? remoteDirectory : null;
        }

        private async Task<bool> CopyUsingPrivateKey(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scpCommand = $"-rqi {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
            var (exitCode, instance) = await Instance.FinishAsync("scp", scpCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode == 0;
        }

        private async Task<bool> CopyUsingSshPass(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var scpCommand = $"-f {passwordFile} scp -rq -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", scpCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode == 0;
            });
        }

        private async Task<bool> CopyImplicitPrivateKey(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scpCommand = $"-rq -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -P {ssh.Port} {sourceDirectory} {ssh.Username}@{ssh.Address}:{remoteDirectory}";
            var (exitCode, instance) = await Instance.FinishAsync("scp", scpCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode == 0;
        }

        private async Task<int> Execute(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            if (!string.IsNullOrEmpty(ssh.Password))
                return await ExecuteUsingSshPass(ssh, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                return await ExecuteUsingPrivateKey(ssh, remoteDirectory, onOutput);
            else
                return await ExecuteImplicitPrivateKey(ssh, remoteDirectory, onOutput);
        }

        private async Task<int> ExecuteUsingPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(remoteDirectory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);
            
            var sshCommand = $"-qtti {ssh.PrivateKeyPath} -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{_executionOptions.Shell} {fileArgument.Replace("\"", "\\\"")}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode;
        }

        private async Task<int> ExecuteUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var scriptFilePath = _scriptTransformer.BuildScriptPath(remoteDirectory);
                var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);
                
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetExecuteCommand(fileArgument)}\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode;
            });
        }

        private async Task<int> ExecuteImplicitPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var scriptFilePath = _scriptTransformer.BuildScriptPath(remoteDirectory);
            var fileArgument = _scriptTransformer.BuildScriptArgument(scriptFilePath);
                
            var sshCommand = $"-o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetExecuteCommand(fileArgument)}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            return exitCode;
        }

        private async Task CleanupRemoteDirectory(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            if (!string.IsNullOrEmpty(ssh.Password))
                await CleanupUsingSshPass(ssh, remoteDirectory, onOutput);
            else if (!string.IsNullOrEmpty(ssh.PrivateKeyPath))
                await CleanupUsingPrivateKey(ssh, remoteDirectory, onOutput);
            else
                await CleanupImplicitPrivateKey(ssh, remoteDirectory, onOutput);
        }

        private async Task CleanupUsingPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var cleanupCommand = $"-o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -i {ssh.PrivateKeyPath} -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetCleanupCommand(remoteDirectory)}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", cleanupCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
            if (exitCode != 0)
            {
                Console.WriteLine(instance);
            }
        }

        private async Task CleanupUsingSshPass(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            await UsePasswordFile(ssh, async passwordFile =>
            {
                var sshCommand = $"-f {passwordFile} ssh -o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetCleanupCommand(remoteDirectory)}\"";
                var (exitCode, instance) = await Instance.FinishAsync("sshpass", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
                return exitCode;
            });
        }

        private async Task CleanupImplicitPrivateKey(SecureShellOptions ssh, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var sshCommand = $"-o StrictHostKeyChecking={(ssh.StrictHostKeyChecking ? "yes" : "no")} -qtt -p {ssh.Port} {ssh.Username}@{ssh.Address} \"{GetCleanupCommand(remoteDirectory)}\"";
            var (exitCode, instance) = await Instance.FinishAsync("ssh", sshCommand, (_, tuple) => onOutput(new ProcessOutput(DateTime.UtcNow, tuple.Data, tuple.Type == DataType.Error)));
        }

        private string GetExecuteCommand(string fileArgument) => $"{_executionOptions.Shell} {fileArgument}";
        private string GetCleanupCommand(string remoteDirectory) => $"rm -r {remoteDirectory}";

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
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;

namespace AgentDeploy.Services.ScriptExecutors
{
    public sealed class SshPassSecureShellExecutor : SecureShellExecutorBase, ISshPassSecureShellExecutor
    {
        private readonly IFileService _fileService;

        public SshPassSecureShellExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService, IFileService fileService) 
            : base(executionOptions, scriptTransformer, processExecutionService)
        {
            _fileService = fileService;
        }
        
        public override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var sourceDir = PathUtils.Combine(ExecutionOptions.DirectorySeparatorChar, ExecutionOptions.TempDir, sourceDirectory);
            var remoteDir = PathUtils.EscapeWhitespaceInPath(remoteDirectory, '\'');
            return await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var scpCommand = $"-f {passwordFilePath} scp -rq {StrictHostKeyChecking(ssh)} -P {ssh.Port} {sourceDirectory} {Credentials(ssh)}:{remoteDir}";
                var result = await ProcessExecutionService.Invoke("sshpass", scpCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode == 0;
            });
        }

        public override async Task<int> Execute(SecureShellOptions ssh, string sourceDirectory, string fileArgument, Action<ProcessOutput> onOutput)
        {
            var sourceDir = PathUtils.Combine(ExecutionOptions.DirectorySeparatorChar, ExecutionOptions.TempDir, sourceDirectory);
            return await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh -qtt {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetExecuteCommand(fileArgument)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        public override async Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var sourceDir = PathUtils.Combine(ExecutionOptions.DirectorySeparatorChar, ExecutionOptions.TempDir, sourceDirectory);
            await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetCleanupCommand(remoteDirectory)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        private async Task<T> UsePasswordFile<T>(SecureShellOptions ssh, string directory, Func<string, Task<T>> task)
        {
            var passwordFile = PathUtils.Combine(ExecutionOptions.DirectorySeparatorChar, directory, "sshpass.txt");
            await _fileService.WriteText(passwordFile, ssh.Password!, CancellationToken.None);
            try
            {
                return await task(passwordFile);
            }
            finally
            {
                
                if (_fileService.FileExists(passwordFile))
                    _fileService.DeleteFile(passwordFile);
            }
        }
    }
}
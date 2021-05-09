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
        private readonly ExecutionOptions _executionOptions;
        private readonly IScriptTransformer _scriptTransformer;
        private readonly IFileService _fileService;

        public SshPassSecureShellExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService, IFileService fileService) 
            : base(executionOptions, scriptTransformer, processExecutionService)
        {
            _executionOptions = executionOptions;
            _scriptTransformer = scriptTransformer;
            _fileService = fileService;
        }
        
        public override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var sourceDir = $"{_executionOptions.TempDir.TrimEnd('/')}{_executionOptions.DirectorySeparatorChar}{sourceDirectory.TrimStart('/')}";
            var remoteDir = _scriptTransformer.EscapeWhitespaceInPath(remoteDirectory, '\'');
            return await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = _scriptTransformer.EscapeWhitespaceInPath(passwordFile);
                var scpCommand = $"-f {passwordFilePath} scp -rq {StrictHostKeyChecking(ssh)} -P {ssh.Port} {sourceDirectory} {Credentials(ssh)}:{remoteDir}";
                var result = await ProcessExecutionService.Invoke("sshpass", scpCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode == 0;
            });
        }

        public override async Task<int> Execute(SecureShellOptions ssh, string sourceDirectory, string fileArgument, Action<ProcessOutput> onOutput)
        {
            var sourceDir = $"{_executionOptions.TempDir.TrimEnd('/')}{_executionOptions.DirectorySeparatorChar}{sourceDirectory.TrimStart('/')}";
            return await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = _scriptTransformer.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh -qtt {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetExecuteCommand(fileArgument)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        public override async Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var sourceDir = $"{_executionOptions.TempDir.TrimEnd('/')}{_executionOptions.DirectorySeparatorChar}{sourceDirectory.TrimStart('/')}";
            await UsePasswordFile(ssh, sourceDir, async passwordFile =>
            {
                var passwordFilePath = _scriptTransformer.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetCleanupCommand(remoteDirectory)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        private async Task<T> UsePasswordFile<T>(SecureShellOptions ssh, string directory, Func<string, Task<T>> task)
        {
            var passwordFile = $"{directory}{_executionOptions.DirectorySeparatorChar}sshpass.txt";
            await _fileService.WriteText(passwordFile, ssh.Password!, CancellationToken.None);
            try
            {
                return await task(passwordFile);
            }
            finally
            {
                if (File.Exists(passwordFile))
                    File.Delete(passwordFile);
            }
        }
    }
}
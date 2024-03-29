using System;
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
        private readonly IOperationContext _operationContext;
        private readonly IFileService _fileService;

        public SshPassSecureShellExecutor(ExecutionOptions executionOptions, IScriptTransformer scriptTransformer, IProcessExecutionService processExecutionService, IFileService fileService, IOperationContext operationContext) 
            : base(executionOptions, scriptTransformer, processExecutionService)
        {
            _fileService = fileService;
            _operationContext = operationContext;
        }

        protected override async Task<bool> Copy(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            var remoteDir = PathUtils.EscapeWhitespaceInPath(remoteDirectory, '\'');
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var scpCommand = $"-f {passwordFilePath} scp -rq {StrictHostKeyChecking(ssh)} -P {ssh.Port} {sourceDirectory} {Credentials(ssh)}:{remoteDir}";
                var result = await ProcessExecutionService.Invoke("sshpass", scpCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode == 0;
            });
        }

        protected override async Task<int> Execute(SecureShellOptions ssh, string remoteDirectory, string fileArgument, Action<ProcessOutput> onOutput,
            CancellationToken cancellationToken)
        {
            return await UsePasswordFile(ssh, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh -qtt {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetExecuteCommand(remoteDirectory, fileArgument)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        protected override async Task Cleanup(SecureShellOptions ssh, string sourceDirectory, string remoteDirectory, Action<ProcessOutput> onOutput)
        {
            await UsePasswordFile(ssh, async passwordFile =>
            {
                var passwordFilePath = PathUtils.EscapeWhitespaceInPath(passwordFile);
                var sshCommand = $"-f {passwordFilePath} ssh {StrictHostKeyChecking(ssh)} -p {ssh.Port} {Credentials(ssh)} \"{GetCleanupCommand(remoteDirectory)}\"";
                var result = await ProcessExecutionService.Invoke("sshpass", sshCommand, (data, error) => onOutput(new ProcessOutput(DateTime.UtcNow, data, error)));
                return result.ExitCode;
            });
        }

        private async Task<T> UsePasswordFile<T>(SecureShellOptions ssh, Func<string, Task<T>> task)
        {
            var passwordFile = PathUtils.Combine(ExecutionOptions.DirectorySeparatorChar, ExecutionOptions.TempDir, $"sshpass-{_operationContext.CorrelationId}.txt");
            await _fileService.WriteTextAsync(passwordFile, ssh.Password!, CancellationToken.None);
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
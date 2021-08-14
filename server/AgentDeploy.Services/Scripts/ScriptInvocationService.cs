using System;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Locking;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptInvocationService : IScriptInvocationService
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IOperationContext _operationContext;
        private readonly IFileService _fileService;
        private readonly IScriptInvocationLockService _scriptInvocationLockService;
        private readonly IScriptInvocationFileService _scriptInvocationFileService;
        private readonly IScriptExecutionService _scriptExecutionService;

        public ScriptInvocationService(
            ExecutionOptions executionOptions,
            IOperationContext operationContext,
            IFileService fileService,
            IScriptExecutionService scriptExecutionService,
            IScriptInvocationLockService scriptInvocationLockService,
            IScriptInvocationFileService scriptInvocationFileService)
        {
            _executionOptions = executionOptions;
            _operationContext = operationContext;
            _fileService = fileService;
            _scriptInvocationLockService = scriptInvocationLockService;
            _scriptInvocationFileService = scriptInvocationFileService;
            _scriptExecutionService = scriptExecutionService;
        }

        public async Task<ExecutionResult> Invoke(ScriptInvocationContext invocationContext)
        {
            var directory = CreateScriptExecutionFolder(invocationContext.CorrelationId, invocationContext.Timestamp);
            try
            {
                using var scriptLock = await _scriptInvocationLockService.Lock(invocationContext.Script, _operationContext.TokenString, _operationContext.OperationCancelled);
                await _scriptInvocationFileService.CopyAssets(invocationContext.Script, directory, _operationContext.OperationCancelled);
                await _scriptInvocationFileService.DownloadFiles(invocationContext, directory, _operationContext.OperationCancelled);
                return await _scriptExecutionService.Execute(invocationContext, directory, _operationContext.OperationCancelled);
            }
            finally
            {
                _fileService.DeleteDirectory(directory, true);
            }
        }


        private string CreateScriptExecutionFolder(Guid correlationId, DateTime timestamp)
        {
            var directoryName = $"agentd_job_{timestamp:yyyyMMddhhmmssfff}_{correlationId}";
            var baseFolder = _executionOptions.TempDir.Replace('\\', '/').TrimEnd('/');
            var directory = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, baseFolder, directoryName);
            _fileService.CreateDirectory(directory);
            return directory;
        }
    }
}
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
            var directory = CreateTemporaryDirectory();
            try
            {
                using var scriptLock = await _scriptInvocationLockService.Lock(invocationContext.Script, _operationContext.TokenString, _operationContext.OperationCancelled);
                await _scriptInvocationFileService.DownloadFiles(invocationContext, directory, _operationContext.OperationCancelled);
                return await _scriptExecutionService.Execute(invocationContext, directory, _operationContext.OperationCancelled);
            }
            finally
            {
                _fileService.DeleteDirectory(directory, true);
            }
        }
        

        private string CreateTemporaryDirectory()
        {
            var directoryName = $"agentd_job_{DateTime.Now:yyyyMMddhhmmssfff}_{Guid.NewGuid()}";
            var directory = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, _executionOptions.TempDir, directoryName);
            _fileService.CreateDirectory(directory);
            return directory;
        }
    }
}
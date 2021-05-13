using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Options;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Scripts
{
    public class ScriptInvocationFileService : IScriptInvocationFileService
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IProcessExecutionService _processExecutionService;
        private readonly IFileService _fileService;
        private readonly ILogger<ScriptInvocationFileService> _logger;

        public ScriptInvocationFileService(ExecutionOptions executionOptions, IProcessExecutionService processExecutionService, IFileService fileService, ILogger<ScriptInvocationFileService> logger)
        {
            _executionOptions = executionOptions;
            _processExecutionService = processExecutionService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task DownloadFiles(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken)
        {
            var filesDirectory = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, directory, "files");
            _fileService.CreateDirectory(filesDirectory);
            foreach (var file in invocationContext.Files)
            {
                var filePath = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, filesDirectory, file.FileName);
                _logger.LogDebug("Downloading {InputFile} to {Path}", file.FileName, filePath);
                
                await using var inputStream = file.OpenRead();
                await _fileService.Write(inputStream, filePath, cancellationToken);
                await ExecuteFilePreprocessing(file, filePath);
                invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, filePath, false));
            }
        }

        private async Task ExecuteFilePreprocessing(AcceptedScriptInvocationFile file, string filePath)
        {
            var preprocessing = file.Preprocessing ?? _executionOptions.DefaultFilePreprocessing;
            if (!string.IsNullOrEmpty(preprocessing))
            {
                _logger.LogDebug("Preprocessing {File} with {Preprocessor}", filePath, preprocessing);
                var preprocess = ReplacementUtils.ReplaceVariable(preprocessing, "FilePath", PathUtils.EscapeWhitespaceInPath(filePath, '\''));
                var arguments = ReplacementUtils.ReplaceVariable(_executionOptions.CommandArgumentFormat, "Command", preprocess);
                var preprocessResult = await _processExecutionService.Invoke(_executionOptions.Shell, arguments, null);
                if (preprocessResult.ExitCode != 0)
                {
                    _logger.LogWarning("Preprocessing of {File} failed with non-zero exit-code {ExitCode}: {Errors}", preprocessResult.ExitCode, preprocessResult.Errors);
                    throw new FilePreprocessingFailedException(file.Name, preprocessResult.ExitCode, preprocessResult.Errors);
                }
            }
        }
    }
}
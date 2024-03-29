using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Scripts;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Scripts
{
    public class ScriptInvocationFileService : IScriptInvocationFileService
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly DirectoryOptions _directoryOptions;
        private readonly IProcessExecutionService _processExecutionService;
        private readonly IFileService _fileService;
        private readonly ILogger<ScriptInvocationFileService> _logger;

        public ScriptInvocationFileService(ExecutionOptions executionOptions, DirectoryOptions directoryOptions, IProcessExecutionService processExecutionService, IFileService fileService, ILogger<ScriptInvocationFileService> logger)
        {
            _executionOptions = executionOptions;
            _directoryOptions = directoryOptions;
            _processExecutionService = processExecutionService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task DownloadFiles(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken)
        {
            var filesFolderName = "files";
            var filesDirectory = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, directory, filesFolderName);
            _fileService.CreateDirectory(filesDirectory);
            foreach (var file in invocationContext.Files)
            {
                if (file.OpenRead != null)
                {
                    await DownloadFile(invocationContext, cancellationToken, filesDirectory, filesFolderName, file);
                }
                else
                {
                    invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, string.Empty, false));   
                }
            }
        }

        public async Task CopyAssets(Script script, string directory,
            CancellationToken cancellationToken)
        {
            var globResults = GlobSearch(script, directory);

            VerifyExistenceOfAssets(globResults);

            foreach (var (sourcePath, destinationPath) in globResults.SelectMany(file => file.Found))
            {
                _logger.LogTrace("Copying required file {InputFile} to {Path}", sourcePath, directory);
                await _fileService.CopyFileAsync(sourcePath, destinationPath, cancellationToken);
            }
        }

        private (string AssetGlob, (string SourcePath, string DestinationPath)[] Found)[] GlobSearch(Script script, string directory)
        {
            var globResults = script.Assets.Distinct().Select(assetGlob =>
            {
                var found = _fileService
                    .FindFiles(_directoryOptions.Assets, assetGlob, true)
                    .Select(file =>
                    {
                        var fileName = Path.GetFileName(file);
                        var destinationPath = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, directory, fileName);
                        return (SourcePath: file, DestinationPath: destinationPath);
                    }).ToArray();
                return (assetGlob, Found: found);
            }).ToArray();

            return globResults;
        }

        private static void VerifyExistenceOfAssets(IEnumerable<(string AssetGlob, (string SourcePath, string DestinationPath)[] found)> assets)
        {
            var missing = assets.Where(asset => !asset.found.Any()).ToArray();
            if (missing.Any())
                throw new AssetGlobSearchFailureException(missing.Select(asset => asset.AssetGlob).ToArray());
        }

        private async Task DownloadFile(ScriptInvocationContext invocationContext, CancellationToken cancellationToken,
            string filesDirectory, string filesFolderName, AcceptedScriptInvocationFile file)
        {
            var filePath = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, filesDirectory, file.FileName);
            _logger.LogTrace("Downloading {InputFile} to {Path}", file.FileName, filePath);

            await using var inputStream = file.OpenRead!();
            await _fileService.WriteAsync(inputStream, filePath, cancellationToken);
            await ExecuteFilePreprocessing(file, filePath);
            var relativeFilePath = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, filesFolderName, Path.GetFileName(filePath));
            invocationContext.Arguments.Add(new AcceptedScriptInvocationArgument(file.Name, relativeFilePath, false));
        }

        private async Task ExecuteFilePreprocessing(AcceptedScriptInvocationFile file, string filePath)
        {
            var preprocessing = file.Preprocessing ?? _executionOptions.DefaultFilePreprocessing;
            if (!string.IsNullOrEmpty(preprocessing))
            {
                _logger.LogTrace("Preprocessing {File} with {Preprocessor}", filePath, preprocessing);
                var preprocess = ReplacementUtils.ReplaceVariable(preprocessing, "FilePath", PathUtils.EscapeWhitespaceInPath(filePath, '\''));
                var arguments = ReplacementUtils.ReplaceVariable(_executionOptions.CommandArgumentFormat, "Command", preprocess);
                var preprocessResult = await _processExecutionService.Invoke(_executionOptions.Shell, arguments, null);
                if (preprocessResult.ExitCode != 0)
                {
                    _logger.LogWarning("Preprocessing of {File} failed with non-zero exit-code {ExitCode}: {Errors}", file.Name, preprocessResult.ExitCode, preprocessResult.Errors);
                    throw new FilePreprocessingFailedException(file.Name, preprocessResult.ExitCode, preprocessResult.Errors);
                }
            }
        }
    }
}
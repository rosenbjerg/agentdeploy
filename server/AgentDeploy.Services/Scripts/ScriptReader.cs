using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Scripts;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptReader : IScriptReader
    {
        private readonly DirectoryOptions _directoryOptions;
        private readonly IDeserializer _deserializer;
        private readonly IFileService _fileService;
        private readonly ILogger<ScriptReader> _logger;

        public ScriptReader(DirectoryOptions directoryOptions, IDeserializer deserializer, IFileService fileService,
            ILogger<ScriptReader> logger)
        {
            _directoryOptions = directoryOptions;
            _deserializer = deserializer;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Script?> Load(string scriptName, CancellationToken cancellationToken)
        {
            var filePath = _fileService.FindFile(_directoryOptions.Scripts, scriptName, "yaml", "yml");
            _logger.LogDebug($"Attempting to read command file: {filePath}");

            var content = await _fileService.ReadAsync(filePath, cancellationToken);
            if (content == null)
                return null;

            var result = _deserializer.Deserialize<Models.Scripts.Script>(content);
            result.Name = scriptName;
            return result;
        }
    }
}
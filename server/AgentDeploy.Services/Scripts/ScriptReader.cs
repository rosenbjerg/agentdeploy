using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services.Scripts
{
    public class ScriptReader : IScriptReader
    {
        private readonly IOperationContext _operationContext;
        private readonly DirectoryOptions _directoryOptions;
        private readonly ILogger<ScriptReader> _logger;
        private readonly IDeserializer _deserializer;
        private readonly IFileReader _fileReader;

        public ScriptReader(IOperationContext operationContext, IDeserializer deserializer, IFileReader fileReader,
            DirectoryOptions directoryOptions, ILogger<ScriptReader> logger)
        {
            _operationContext = operationContext;
            _deserializer = deserializer;
            _fileReader = fileReader;
            _directoryOptions = directoryOptions;
            _logger = logger;
        }

        public async Task<Models.Scripts.Script?> Load(string scriptName)
        {
            var filePath = _fileReader.FindFile(_directoryOptions.Scripts, scriptName, "yaml", "yml");
            _logger.LogDebug($"Attempting to read command file: {filePath}");

            var content = await _fileReader.ReadAsync(filePath, _operationContext.OperationCancelled);
            if (content == null)
                return null;

            var result = _deserializer.Deserialize<Models.Scripts.Script>(content);
            result.Name = scriptName;
            return result;
        }
    }
}
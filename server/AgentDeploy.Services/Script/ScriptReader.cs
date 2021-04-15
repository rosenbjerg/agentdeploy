using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services.Script
{
    public class ScriptReader
    {
        private readonly IOperationContext _operationContext;
        private readonly DirectoryOptions _directoryOptions;
        private readonly ILogger<ScriptReader> _logger;
        private readonly IDeserializer _deserializer;

        public ScriptReader(IOperationContext operationContext, IDeserializer deserializer,
            DirectoryOptions directoryOptions, ILogger<ScriptReader> logger)
        {
            _operationContext = operationContext;
            _deserializer = deserializer;
            _directoryOptions = directoryOptions;
            _logger = logger;
        }

        public async Task<Models.Scripts.Script?> Load(string command)
        {
            var filePath = Path.Combine(_directoryOptions.Scripts, $"{command}.yaml");
            _logger.LogDebug($"Attempting to read command file: {filePath}");

            if (!File.Exists(filePath))
                return null;

            return await PerformanceLoggingUtilities.Time($"Parsing command file {command}", _logger, async () =>
            {
                var yaml = await File.ReadAllTextAsync(filePath, _operationContext.OperationCancelled);
                return _deserializer.Deserialize<Models.Scripts.Script>(yaml);
            });
        }
    }
}
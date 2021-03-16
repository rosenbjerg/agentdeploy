using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Models;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services
{
    public class CommandReader
    {
        private readonly IOperationContext _operationContext;
        private readonly DirectoryOptions _directoryOptions;
        private readonly ILogger<CommandReader> _logger;
        private readonly IDeserializer _deserializer;

        public CommandReader(IOperationContext operationContext, IDeserializer deserializer,
            DirectoryOptions directoryOptions, ILogger<CommandReader> logger)
        {
            _operationContext = operationContext;
            _deserializer = deserializer;
            _directoryOptions = directoryOptions;
            _logger = logger;
        }

        public async Task<Script?> Load(string command)
        {
            var filePath = Path.Combine(_directoryOptions.Scripts, $"{command}.yaml");
            _logger.LogDebug($"Attempting to read command file: {filePath}");

            if (!File.Exists(filePath))
                return null;

            return await PerformanceLoggingUtilities.Time($"Parsing command file {command}", _logger, async () =>
            {
                var yaml = await File.ReadAllTextAsync(filePath, _operationContext.OperationCancelled);
                return _deserializer.Deserialize<Script>(yaml);
            });
        }
    }
}
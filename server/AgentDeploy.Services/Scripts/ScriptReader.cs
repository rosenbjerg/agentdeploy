using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Exceptions;
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
            _logger.LogDebug($"Attempting to read script file: {filePath}");

            var content = await _fileService.ReadAsync(filePath, cancellationToken);
            if (content == null)
                return null;

            var result = _deserializer.Deserialize<Script>(content);
            result.Name = scriptName;
            
            ValidateVariableUsage(result);

            return result;
        }

        private static void ValidateVariableUsage(Script script)
        {
            var declaredReplacements = script.Variables.Select(v => v.Key).Concat(script.Files.Select(f => f.Key)).ToArray();
            var usedReplacements = ReplacementUtils.ExtractUsedVariables(script.Command);

            var unusedReplacements = declaredReplacements.Where(dr => !usedReplacements.Contains(dr)).Distinct().ToArray();
            var undeclaredReplacements = usedReplacements.Where(ur => !declaredReplacements.Contains(ur)).Distinct().ToArray();

            if (unusedReplacements.Any() || undeclaredReplacements.Any())
            {
                var errors = unusedReplacements.Select(ur => (ur, "Variable is declared but not used"))
                    .Concat(undeclaredReplacements.Select(ur => (ur, "Variable is used but not declared")))
                    .ToDictionary(urp => urp.ur, urp => new[] { urp.Item2 });
                throw new InvalidScriptFileException(errors);
            }
        }
    }
}
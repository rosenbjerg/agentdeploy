using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services
{
    public class TokenReader : ITokenReader
    {
    private readonly DirectoryOptions _directoryOptions;
    private readonly ILogger<TokenReader> _logger;
    private readonly IDeserializer _deserializer;

    public TokenReader(DirectoryOptions directoryOptions, IDeserializer deserializer, ILogger<TokenReader> logger)
    {
        _directoryOptions = directoryOptions;
        _deserializer = deserializer;
        _logger = logger;
    }

    public async Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_directoryOptions.Tokens, $"{token}.yaml");
        _logger.LogDebug($"Attempting to read token file: {filePath}");

        if (!File.Exists(filePath))
            return null;

        return await PerformanceLoggingUtilities.Time($"Parsing token file {token}", _logger, async () =>
        {
            var yaml = await File.ReadAllTextAsync(filePath, cancellationToken);
            return _deserializer.Deserialize<Token>(yaml);
        });
    }
    }
}
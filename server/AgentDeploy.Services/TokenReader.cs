using System.Threading;
using System.Threading.Tasks;
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
        private readonly IFileReader _fileReader;

        public TokenReader(DirectoryOptions directoryOptions, IDeserializer deserializer, IFileReader fileReader, ILogger<TokenReader> logger)
        {
            _directoryOptions = directoryOptions;
            _deserializer = deserializer;
            _fileReader = fileReader;
            _logger = logger;
        }

        public async Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken)
        {
            var filePath = _fileReader.FindFile(_directoryOptions.Tokens, token, "yaml", "yml");
            _logger.LogDebug($"Attempting to read token file: {filePath}");
            var content = await _fileReader.ReadAsync(filePath, cancellationToken);
            if (content == null)
                return null;
            
            return _deserializer.Deserialize<Token>(content);
        }
    }
}
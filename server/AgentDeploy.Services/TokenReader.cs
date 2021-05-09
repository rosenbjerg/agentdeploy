using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AgentDeploy.Services
{
    public sealed class TokenReader : ITokenReader
    {
        private readonly DirectoryOptions _directoryOptions;
        private readonly ILogger<TokenReader> _logger;
        private readonly IDeserializer _deserializer;
        private readonly IFileService _fileService;

        public TokenReader(DirectoryOptions directoryOptions, IDeserializer deserializer, IFileService fileService, ILogger<TokenReader> logger)
        {
            _directoryOptions = directoryOptions;
            _deserializer = deserializer;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken)
        {
            var filePath = _fileService.FindFile(_directoryOptions.Tokens, token, "yaml", "yml");
            _logger.LogDebug($"Attempting to read token file: {filePath}");
            var content = await _fileService.ReadAsync(filePath, cancellationToken);
            if (content == null)
                return null;
            
            return _deserializer.Deserialize<Token>(content);
        }
    }
}
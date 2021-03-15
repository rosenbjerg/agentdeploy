using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Services
{
    public class TokenFileParser
    {
        private readonly DirectoryOptions _directoryOptions;
        private readonly ILogger<TokenFileParser> _logger;
        private readonly IDeserializer _deserializer;

        public TokenFileParser(DirectoryOptions directoryOptions, ILogger<TokenFileParser> logger)
        {
            _directoryOptions = directoryOptions;
            _logger = logger;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        public async Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken = default)
        {
            var filename = Path.Combine(_directoryOptions.Tokens, $"{token}.yaml");
            _logger.LogInformation("Loading token file: {TokenFile}", filename);
            if (!File.Exists(filename))
                return null;
            var yaml = await File.ReadAllTextAsync(filename, cancellationToken);
            return _deserializer.Deserialize<Token>(yaml);
        }
    }
}
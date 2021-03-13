using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Services
{
    public class TokenFileParser
    {
        private readonly DirectoryOptions _directoryOptions;
        private readonly IDeserializer _deserializer;

        public TokenFileParser(DirectoryOptions directoryOptions)
        {
            _directoryOptions = directoryOptions;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        public async Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken = default)
        {
            var filename = Path.Combine(_directoryOptions.Tokens, $"{token}.yaml");
            if (!File.Exists(filename))
                return null;
            var yaml = await File.ReadAllTextAsync(filename, cancellationToken);
            return _deserializer.Deserialize<Token>(yaml);
        }
    }
}
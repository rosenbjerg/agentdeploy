using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Application.Parser.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Application.Parser
{
    public class TokenFileParser
    {
        private readonly IDeserializer _deserializer;

        public TokenFileParser()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        public async Task<Token?> ParseTokenFile(string token)
        {
            var filename = Path.Combine("tokens", $"{token}.yaml");
            if (!File.Exists(filename))
                return null;
            var yaml = await File.ReadAllTextAsync(filename);
            return _deserializer.Deserialize<Token>(yaml);
        }
    }
}
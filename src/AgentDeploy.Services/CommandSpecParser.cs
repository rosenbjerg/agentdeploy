using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Services
{
    public class CommandSpecParser
    {
        private readonly IDeserializer _deserializer;

        public CommandSpecParser()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        public async Task<Script?> Load(string command, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine("scripts", $"{command}.yaml");
            if (!File.Exists(path))
                return null;
            
            var yaml = await File.ReadAllTextAsync(path, cancellationToken);
            return _deserializer.Deserialize<Script>(yaml);
        }
    }
}
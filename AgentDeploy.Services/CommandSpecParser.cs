using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Application.Parser.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Application.Parser
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
        public async Task<Script?> Load(string command)
        {
            var path = Path.Combine("scripts", $"{command}.yaml");
            if (!File.Exists(path))
                return null;
            
            var yaml = await File.ReadAllTextAsync(path);
            return _deserializer.Deserialize<Script>(yaml);
        }
    }
}
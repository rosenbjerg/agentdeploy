using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Services
{
    public class CommandSpecParser
    {
        private readonly DirectoryOptions _directoryOptions;
        private readonly IDeserializer _deserializer;
        
        public CommandSpecParser(DirectoryOptions directoryOptions)
        {
            _directoryOptions = directoryOptions;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
        }
        public async Task<Script?> Load(string command, CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(_directoryOptions.Scripts, $"{command}.yaml");
            Console.WriteLine("Scripts: " + path);
            if (!File.Exists(path))
                return null;
            
            var yaml = await File.ReadAllTextAsync(path, cancellationToken);
            return _deserializer.Deserialize<Script>(yaml);
        }
    }
}
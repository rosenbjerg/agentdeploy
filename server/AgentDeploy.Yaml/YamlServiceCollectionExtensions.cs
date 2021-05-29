using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Yaml
{
    public static class YamlServiceCollectionExtensions
    {
        public static IServiceCollection AddYamlParser(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton(_ => new DeserializerBuilder()
                    .WithTypeConverter(new ExtendedYamlEnumConverter())
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build());
        }
    }
}
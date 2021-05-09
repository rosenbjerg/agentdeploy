using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace AgentDeploy.Yaml
{
    public class YamlStringEnumConverter : IYamlTypeConverter
    {
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>> TypeCache = new();
        
        public bool Accepts(Type type) => type.IsEnum && type.GetCustomAttribute<CustomYamlEnumAttribute>() != null;

        public object ReadYaml(IParser parser, Type type)
        {
            var parsedEnum = parser.Consume<Scalar>();

            if (!TypeCache.TryGetValue(type, out var serializableValues))
            {
                serializableValues = type.GetMembers()
                    .Where(member => member.MemberType == MemberTypes.Field && member.Name != "value__")
                    .SelectMany(member =>
                    {
                        var attributes = member.GetCustomAttributes<YamlEnumMemberAliasAttribute>(false);
                        var names = attributes.Select(attr => attr.Name).Concat(new[] { member.Name.ToLowerInvariant() });
                        return names.Select(n => (name: n, member));
                    })
                    .Where(pa => !string.IsNullOrEmpty(pa.name))
                    .ToDictionary(pa => pa.name, pa => pa.member);
                
                TypeCache[type] = serializableValues;
            }

            if (!serializableValues.TryGetValue(parsedEnum.Value, out var memberInfo))
                throw new YamlException(parsedEnum.Start, parsedEnum.End, $"Value '{parsedEnum.Value}' not found in enum '{type.Name}'");

            return Enum.Parse(type, memberInfo.Name);
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
            
            var enumMember = type.GetMember(value.ToString()).FirstOrDefault();
            var yamlValue =
                enumMember?.GetCustomAttributes<YamlEnumMemberAliasAttribute>(true).Select(ema => ema.Name).FirstOrDefault() ??
                value.ToString();
            emitter.Emit(new Scalar(yamlValue));
        }
    }
}
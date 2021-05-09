using System;

namespace AgentDeploy.Yaml
{
    public class YamlEnumMemberAliasAttribute : Attribute
    {
        public string Name { get; }

        public YamlEnumMemberAliasAttribute(string name)
        {
            Name = name;
        }
    }
}
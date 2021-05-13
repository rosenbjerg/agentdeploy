using System;

namespace AgentDeploy.Yaml
{
    public class ExtendedYamlEnumMember : Attribute
    {
        public string[] Aliases { get; }

        public ExtendedYamlEnumMember(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}
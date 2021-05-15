using System;

namespace AgentDeploy.Yaml
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ExtendedYamlEnumMemberAttribute : Attribute
    {
        public string[] Aliases { get; }

        public ExtendedYamlEnumMemberAttribute(params string[] aliases)
        {
            Aliases = aliases;
        }
    }
}
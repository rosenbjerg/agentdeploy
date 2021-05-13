using AgentDeploy.Yaml;

namespace AgentDeploy.Models.Scripts
{
    [CustomYamlEnum]
    public enum ScriptArgumentType
    {
        String,
        
        [ExtendedYamlEnumMember("int")]
        Integer,
        
        [ExtendedYamlEnumMember("float")]
        Decimal,
        
        [ExtendedYamlEnumMember("bool")]
        Boolean
    }
}
using AgentDeploy.Yaml;

namespace AgentDeploy.Models.Scripts
{
    [ExtendedYamlEnum]
    public enum ScriptArgumentType
    {
        FQDN,
        
        String,
        
        [ExtendedYamlEnumMember("int")]
        Integer,
        
        [ExtendedYamlEnumMember("float")]
        Decimal,
        
        [ExtendedYamlEnumMember("bool")]
        Boolean
    }
}
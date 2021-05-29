using AgentDeploy.Yaml;

namespace AgentDeploy.Models.Tokens
{
    [ExtendedYamlEnum]
    public enum HostKeyCheckingOptions
    {
        [ExtendedYamlEnumMember]
        AcceptNew,
        
        [ExtendedYamlEnumMember("true")]
        Yes,
        
        [ExtendedYamlEnumMember("false")]
        No,
        
        Off,
    }
}
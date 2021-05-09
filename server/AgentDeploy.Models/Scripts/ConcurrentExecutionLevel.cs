using AgentDeploy.Yaml;

namespace AgentDeploy.Models.Scripts
{
    [CustomYamlEnum]
    public enum ConcurrentExecutionLevel
    {
        /// <summary>
        /// Concurrent execution allowed (default)
        /// </summary>
        Full,
        /// <summary>
        /// Concurrent execution of script allowed for distinct tokens
        /// </summary>
        [YamlEnumMemberAlias("per-token")]
        PerToken,
        /// <summary>
        /// Concurrent execution not allowed
        /// </summary>
        None
    }
}
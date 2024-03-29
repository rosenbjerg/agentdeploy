using AgentDeploy.Yaml;

namespace AgentDeploy.Models.Scripts
{
    [ExtendedYamlEnum]
    public enum ConcurrentExecutionLevel
    {
        /// <summary>
        /// Concurrent execution allowed (default)
        /// </summary>
        Full,
        /// <summary>
        /// Concurrent execution of script allowed for distinct tokens
        /// </summary>
        [ExtendedYamlEnumMember]
        PerToken,
        /// <summary>
        /// Concurrent execution not allowed
        /// </summary>
        None
    }
}
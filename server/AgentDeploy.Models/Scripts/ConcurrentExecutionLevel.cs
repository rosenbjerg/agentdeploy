namespace AgentDeploy.Models.Scripts
{
    public enum ConcurrentExecutionLevel
    {
        /// <summary>
        /// Concurrent execution allowed (default)
        /// </summary>
        Full,
        /// <summary>
        /// Concurrent execution of script allowed for distinct tokens
        /// </summary>
        PerToken,
        /// <summary>
        /// Concurrent execution not allowed 
        /// </summary>
        None
    }
}
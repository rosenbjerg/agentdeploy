namespace AgentDeploy.Models.Options
{
    public class ExecutionOptions
    {
        /// <summary>
        /// Which shell to execute the command file with
        /// </summary>
        public string Shell { get; set; } = "bash";
        
        /// <summary>
        /// Whether to replace C:\ in absolute path with /mnt/c/ and replace \ with / in paths
        /// </summary>
        public bool UseWslPath { get; set; } = true;
    }
}
namespace AgentDeploy.Models.Options
{
    public class DirectoryOptions
    {
        /// <summary>
        /// Directory containing yaml script files
        /// </summary>
        public string Scripts { get; set; } = "scripts";
        
        /// <summary>
        /// Directory containing yaml token files
        /// </summary>
        public string Tokens { get; set; } = "tokens";
    }
}
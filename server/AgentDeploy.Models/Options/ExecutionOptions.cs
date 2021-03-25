namespace AgentDeploy.Models.Options
{
    public class ExecutionOptions
    {
        /// <summary>
        /// Which shell to execute the command file with
        /// </summary>
        public string Shell { get; set; } = "/bin/sh";
        
        /// <summary>
        /// Optionally set the file extension used for the script file. Not necessary for most shells
        /// </summary>
        public string ShellFileExtension { get; set; } = ".sh";

        /// <summary>
        /// Optional formatting of the script path argument, necessary for some shells
        /// </summary>
        public string FileArgumentFormat { get; set; } = "$(ScriptPath)";
    }
}
using System;
using System.IO;

namespace AgentDeploy.Models.Options
{
    public class ExecutionOptions
    {
        private static readonly bool ClamAvEnabled = Environment.GetEnvironmentVariable("CLAMAV") == "true";

        public char DirectorySeparatorChar { get; set; } = Path.DirectorySeparatorChar;
        public string TempDir { get; set; } = Path.GetTempPath();
        
        public string? DefaultFilePreprocessing { get; set; } = ClamAvEnabled ? "clamscan -i $(FilePath)" : null;
        
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
        
        public string EnvironmentVariableFormat { get; set; } = "$(Key)=$(Value)";
    }
}
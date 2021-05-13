using System;
using System.IO;

namespace AgentDeploy.Models.Options
{
    public class ExecutionOptions
    {
        private static readonly bool ClamAvEnabled = Environment.GetEnvironmentVariable("CLAMAV") == "true";

        /// <summary>
        /// Specify the string to use as linebreak. Defaults to Environment.NewLine
        /// </summary>
        public string Linebreak { get; set; } = Environment.NewLine;
        
        /// <summary>
        /// Character used as directory separator. Defaults to Path.DirectorySeparatorChar
        /// </summary>
        public char DirectorySeparatorChar { get; set; } = Path.DirectorySeparatorChar;
        
        /// <summary>
        /// Directory used for saving temporary files, such as the generated script and any uploaded files. Defaults to Path.GetTempPath
        /// </summary>
        public string TempDir { get; set; } = Path.GetTempPath();
        
        public string? DefaultFilePreprocessing { get; set; } = ClamAvEnabled ? "clamscan -i $(FilePath)" : null;
        
        /// <summary>
        /// The name or path of the shell to use. Defaults to /bin/sh
        /// </summary>
        public string Shell { get; set; } = "/bin/sh";
        
        /// <summary>
        /// The file extension used for the script file. Defaults to .sh
        /// </summary>
        public string ShellFileExtension { get; set; } = ".sh";

        /// <summary>
        /// Argument format for the specified shell for executing a script file
        /// </summary>
        public string FileArgumentFormat { get; set; } = "$(ScriptPath)";
        
        /// <summary>
        /// Argument format for the specified shell for executing an inline command
        /// </summary>
        public string CommandArgumentFormat { get; set; } = "-c \"$(Command)\"";
        
        /// <summary>
        /// Format to use for environment variables prepended to the command of the script
        /// </summary>
        public string EnvironmentVariableFormat { get; set; } = "$(Key)=$(Value)";
    }
}
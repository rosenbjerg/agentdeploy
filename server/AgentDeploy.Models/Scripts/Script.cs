using System.Collections.Generic;

namespace AgentDeploy.Models.Scripts
{
    public class Script
    {
        /// <summary>
        /// Initialized to the name of the script file (without extension) by the script parser
        /// </summary>
        public string Name { get; set; } = null!;
        
        /// <summary>
        /// Variables needed for invocation of script
        /// </summary>
        public Dictionary<string, ScriptVariableDefinition?> Variables { get; init; } = new();
        
        /// <summary>
        /// Files needed for invocation of script
        /// </summary>
        public Dictionary<string, ScriptFileDefinition?> Files { get; init; } = new();

        /// <summary>
        /// File assets necessary for script execution. File paths must be relative to working directory or absolute.
        /// Will be copied to script execution working directory
        /// </summary>
        public List<string> Assets { get; init; } = new();
        
        /// <summary>
        /// The command to run when executing this script
        /// </summary>
        public string Command { get; init; } = null!;
        
        /// <summary>
        /// Whether to include command in response
        /// </summary>
        public bool ShowCommand { get; init; } = false;
        
        /// <summary>
        /// Whether to include output in response
        /// </summary>
        public bool ShowOutput { get; init; } = true;

        /// <summary>
        /// Specifies the level of concurrent execution that is allowed
        /// </summary>
        public ConcurrentExecutionLevel Concurrency { get; init; } = ConcurrentExecutionLevel.Full;
    }
}
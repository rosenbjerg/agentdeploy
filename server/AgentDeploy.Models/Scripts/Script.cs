using System.Collections.Generic;

namespace AgentDeploy.Models.Scripts
{
    public class Script
    {
        /// <summary>
        /// Variables needed for invocation of script
        /// </summary>
        public Dictionary<string, ScriptArgumentDefinition> Variables { get; set; } = new();
        
        /// <summary>
        /// Files needed for invocation of script
        /// </summary>
        public Dictionary<string, ScriptFileArgument> Files { get; set; } = new();
        
        /// <summary>
        /// The command to run when executing the script
        /// </summary>
        public string Command { get; set; } = null!;
        
        /// <summary>
        /// Whether to include command in response
        /// </summary>
        public bool ShowCommand { get; set; } = false;
        
        /// <summary>
        /// Whether to include output in response
        /// </summary>
        public bool ShowOutput { get; set; } = true;
    }
}
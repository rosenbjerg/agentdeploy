using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class Script
    {
        public Dictionary<string, ScriptArgument> Variables { get; set; } = new();
        public Dictionary<string, ScriptFileArgument> Files { get; set; } = new();
        public string Command { get; set; } = null!;
        public bool ShowCommand { get; set; } = false;
        public bool ShowOutput { get; set; } = true;
    }
}
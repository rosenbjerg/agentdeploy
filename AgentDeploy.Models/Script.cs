using System.Collections.Generic;

namespace AgentDeploy.Services.Models
{
    public class Script
    {
        public Dictionary<string, ScriptArgument> Variables { get; set; } = new();
        public string Name { get; set; } = null!;
        public string Command { get; set; } = null!;
        public bool ShowCommand { get; set; } = false;
        public bool ShowOutput { get; set; } = true;
    }
}
using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class Token
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, ConstrainedCommand> AvailableCommands { get; set; } = new();
    }
}
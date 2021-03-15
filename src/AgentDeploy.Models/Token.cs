using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class Token
    {
        public string? Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public Dictionary<string, ConstrainedCommand> AvailableCommands { get; set; } = new();
    }
}
using System.Collections.Generic;

namespace AgentDeploy.Services.Models
{
    public class Token
    {
        public Dictionary<string, ConstrainedCommand> AvailableCommands { get; set; } = new();
    }

    public class ConstrainedCommand
    {
        public Dictionary<string, string> VariableContraints { get; set; } = new();
        public Dictionary<string, string> LockedVariables { get; set; } = new();
    }
}
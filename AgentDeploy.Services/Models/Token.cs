using System.Collections.Generic;

namespace AgentDeploy.Application.Parser.Models
{
    public class Token
    {
        public Dictionary<string, CommandArgumentConstraint> AvailableCommands { get; set; } = new();
    }

    public class CommandArgumentConstraint
    {
        public Dictionary<string, string> VariableContraints { get; set; } = new();
    }
}
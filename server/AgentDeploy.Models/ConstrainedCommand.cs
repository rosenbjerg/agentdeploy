using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class ConstrainedCommand
    {
        public SecureShellOptions? Ssh { get; set; }
        public Dictionary<string, string> VariableContraints { get; set; } = new();
        public Dictionary<string, string> LockedVariables { get; set; } = new();
    }
}
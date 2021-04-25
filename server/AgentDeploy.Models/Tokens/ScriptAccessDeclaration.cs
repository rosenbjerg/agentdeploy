using System.Collections.Generic;

namespace AgentDeploy.Models.Tokens
{
    public class ScriptAccessDeclaration
    {
        public SecureShellOptions? Ssh { get; set; }
        public Dictionary<string, string> VariableContraints { get; set; } = new();
        public Dictionary<string, string> LockedVariables { get; set; } = new();
    }
}
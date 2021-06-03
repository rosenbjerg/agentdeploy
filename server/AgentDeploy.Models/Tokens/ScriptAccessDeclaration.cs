using System.Collections.Generic;

namespace AgentDeploy.Models.Tokens
{
    public class ScriptAccessDeclaration
    {
        public SecureShellOptions? Ssh { get; init; }
        public Dictionary<string, string> VariableConstraints { get; init; } = new();
        public Dictionary<string, string> LockedVariables { get; init; } = new();
    }
}
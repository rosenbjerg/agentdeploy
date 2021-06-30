using System;
using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class ParsedScriptInvocation
    {
        public string ScriptName { get; set; } = null!;
        public Guid? WebsocketSessionId { get; set; }

        public Dictionary<string, ScriptInvocationVariable> Variables { get; set; } = new();
        public ScriptEnvironmentVariable[] EnvironmentVariables { get; set; } = Array.Empty<ScriptEnvironmentVariable>();

        public Dictionary<string, ScriptInvocationFile> Files { get; set; } = new();
    }
}
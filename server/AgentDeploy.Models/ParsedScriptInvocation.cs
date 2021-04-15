using System;
using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class ParsedScriptInvocation
    {
        public string ScriptName { get; set; } = null!;
        public Guid? WebsocketSessionId { get; set; }

        public Dictionary<string, ScriptInvocationVariable> Variables { get; set; } = null!;
        public ScriptEnvironmentVariable[] EnvironmentVariables { get; set; } = null!;

        public Dictionary<string, ScriptInvocationFile> Files { get; set; } = null!;
    }
}
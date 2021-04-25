using System;
using System.Collections.Generic;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Models
{
    public class ScriptInvocationContext
    {
        public Script Script { get; set; } = null!;
        public List<AcceptedScriptInvocationArgument> Arguments { get; set; } = new(0);
        public AcceptedScriptInvocationFile[] Files { get; set; } = Array.Empty<AcceptedScriptInvocationFile>();
        public ScriptEnvironmentVariable[] EnvironmentVariables { get; set; } = Array.Empty<ScriptEnvironmentVariable>();
        public SecureShellOptions? SecureShellOptions { get; set; }
        public Guid? WebSocketSessionId { get; set; }
    }
}
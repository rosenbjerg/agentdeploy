using System;
using System.Collections.Generic;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Models
{
    public class ScriptInvocationContext
    {
        public Script Script { get; init; } = null!;
        public List<AcceptedScriptInvocationArgument> Arguments { get; init; } = new(0);
        public AcceptedScriptInvocationFile[] Files { get; init; } = Array.Empty<AcceptedScriptInvocationFile>();
        public ScriptEnvironmentVariable[] EnvironmentVariables { get; init; } = Array.Empty<ScriptEnvironmentVariable>();
        public SecureShellOptions? SecureShellOptions { get; init; }
        public Guid? WebSocketSessionId { get; init; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public Guid CorrelationId { get; init; }
    }
}
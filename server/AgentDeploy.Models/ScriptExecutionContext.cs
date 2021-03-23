using System;
using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public class ScriptExecutionContext
    {
        public Script Script { get; set; } = null!;
        public List<InvocationArgument> Arguments { get; set; } = new(0);
        public InvocationFile[] Files { get; set; } = Array.Empty<InvocationFile>();
        public string[] EnvironmentVariables { get; set; } = Array.Empty<string>();
        public SecureShellOptions? SecureShellOptions { get; set; }
        public Guid? WebSocketSessionId { get; set; }
    }
}
using System.Collections.Generic;

namespace AgentDeploy.Services
{
    public sealed record ProcessExecutionResult(int ExitCode, IReadOnlyList<string> Output, IReadOnlyList<string> Errors);
}
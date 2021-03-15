using System.Collections.Generic;

namespace AgentDeploy.Services
{
    public record ExecutionResult(IEnumerable<ProcessOutput> Output, string? Command, int ExitCode);
}
using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public record ExecutionResult(IEnumerable<ProcessOutput> Output, string? Script, int ExitCode);
}
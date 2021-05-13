using System.Collections.Generic;

namespace AgentDeploy.Models
{
    public record ExecutionResult(IEnumerable<ProcessOutput> Output, IEnumerable<string> Script, int ExitCode);
}
using System;

namespace AgentDeploy.Models
{
    public record ProcessOutput(DateTime Timestamp, string Output, bool Error);
}
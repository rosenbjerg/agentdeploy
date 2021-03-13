using System;

namespace AgentDeploy.Services
{
    public record ProcessOutput(DateTime Timestamp, string Output, bool Error);
}
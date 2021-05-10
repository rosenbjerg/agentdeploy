using System.Collections.Generic;

namespace AgentDeploy.ExternalApi
{
    public record FailedInvocation
    {
        public string Title { get; set; } = null!;
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}
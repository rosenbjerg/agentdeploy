using System;
using System.Threading;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Models
{
    public class OperationContext : IOperationContext
    {
        public CancellationToken OperationCancelled { get; set; }
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public Token Token { get; set; } = null!;
        public string TokenString { get; set; } = null!;
    }
}
using System;
using System.Net;
using System.Threading;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Models
{
    public class OperationContext : IOperationContext
    {
        public CancellationToken OperationCancelled { get; set; }
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public Token Token { get; set; } = null!;
        public string TokenString { get; set; } = null!;
        public IPAddress ClientIp { get; set; } = null!;
    }
}
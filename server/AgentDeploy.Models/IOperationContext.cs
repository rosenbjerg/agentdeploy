using System;
using System.Net;
using System.Threading;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Models
{
    public interface IOperationContext
    {
        public CancellationToken OperationCancelled { get; }
        public Guid CorrelationId { get; }
        public Token Token { get; }
        public string TokenString { get; }
        public IPAddress ClientIp { get; }
    }
}
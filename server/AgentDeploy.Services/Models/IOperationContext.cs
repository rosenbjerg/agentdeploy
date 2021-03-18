using System;
using System.Threading;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Models
{
    public interface IOperationContext
    {
        public CancellationToken OperationCancelled { get; }
        public Guid CorrelationId { get; }
        public Token Token { get; }
        public string TokenString { get; }
    }
}
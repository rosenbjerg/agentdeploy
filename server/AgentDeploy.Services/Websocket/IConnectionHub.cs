using System;
using System.Threading.Tasks;
using AgentDeploy.Models.Websocket;

namespace AgentDeploy.Services.Websocket
{
    public interface IConnectionHub
    {
        ConnectionContext PrepareSession(Guid webSocketSessionId);
        Task<bool> JoinSession(Guid webSocketSessionId, Connection connection);
    }
}
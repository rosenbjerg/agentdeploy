using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgentDeploy.Models.Websocket;

namespace AgentDeploy.Services.Websocket
{
    public class ConnectionHub : IConnectionHub
    {
        private readonly ConcurrentDictionary<Guid, ConnectionContext> _connectionTable = new();

        public async Task<bool> JoinSession(Guid webSocketSessionId, Connection connection)
        {
            var session = await AwaitSession(webSocketSessionId, 0.5f);
            if (session == null)
                return false;
            
            session.SetConnection(connection);
            return true;
        }

        private async Task<ConnectionContext?> AwaitSession(Guid webSocketSessionId, float timeoutSeconds)
        {
            var stepSize = 100;
            var steps = (timeoutSeconds * 1000) / stepSize;
            for (var i = 0; i < steps; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(stepSize));
                if (_connectionTable.TryGetValue(webSocketSessionId, out var session))
                    return session;
            }

            return null;
        }

        public ConnectionContext PrepareSession(Guid webSocketSessionId)
        {
            var ctx = new ConnectionContext();
            _connectionTable[webSocketSessionId] = ctx;
            ctx.Disconnected += (_, _) => _connectionTable.TryRemove(webSocketSessionId, out _);
            return ctx;
        }
    }
}
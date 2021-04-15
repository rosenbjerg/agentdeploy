using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgentDeploy.Models.Websocket;

namespace AgentDeploy.Services.Websocket
{
    public class ConnectionHub
    {
        private readonly ConcurrentDictionary<Guid, ConnectionContext> _connectionTable = new();

        public async Task<bool> FillBooth(Guid webSocketSessionId, Connection connection)
        {
            var booth = await AwaitBooth(webSocketSessionId, 0.5f);
            if (booth == null)
                return false;
            
            booth.SetConnection(connection);
            return true;
        }

        private async Task<ConnectionContext?> AwaitBooth(Guid webSocketSessionId, float timeoutSeconds)
        {
            var stepSize = 100;
            var steps = (timeoutSeconds * 1000) / stepSize;
            for (var i = 0; i < steps; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(stepSize));
                if (_connectionTable.TryGetValue(webSocketSessionId, out var booth))
                    return booth;
            }

            return null;
        }

        public ConnectionContext Prepare(Guid webSocketSessionId)
        {
            var ctx = new ConnectionContext();
            _connectionTable[webSocketSessionId] = ctx;
            ctx.Disconnected += (_, _) => _connectionTable.TryRemove(webSocketSessionId, out _);
            return ctx;
        }
    }
}
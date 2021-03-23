using System;
using System.Collections.Concurrent;
using AgentDeploy.Services.Models;

namespace AgentDeploy.Services
{
    public class ConnectionHub
    {
        private readonly ConcurrentDictionary<Guid, ConnectionContext> _connectionTable = new();

        public bool FillBooth(Guid webSocketSessionId, Connection connection)
        {
            if (!_connectionTable.TryGetValue(webSocketSessionId, out var booth))
                return false;
            
            booth.SetConnection(connection);
            return true;
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
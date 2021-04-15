using System;
using System.Threading.Tasks;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Websocket;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.Services.Websocket
{
    public abstract class ConnectionAccepter : IConnectionAccepter
    {
        private readonly ConnectionHub _connectionHub;

        protected ConnectionAccepter(ConnectionHub connectionHub)
        {
            _connectionHub = connectionHub;
        }

        protected async Task Connect(Guid sessionId, Connection connection)
        {
            var connected = await _connectionHub.FillBooth(sessionId, connection);
            if (!connected)
                throw new WebsocketSessionNotFoundException(nameof(sessionId));
        }

        public abstract Task Accept(HttpContext httpContext, Guid sessionId);
    }
}
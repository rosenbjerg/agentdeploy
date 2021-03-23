using System;
using System.Threading.Tasks;
using AgentDeploy.Services;
using AgentDeploy.Services.Models;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.ExternalApi.Websocket
{
    public abstract class ConnectionAccepter
    {
        private readonly ConnectionHub _connectionHub;

        protected ConnectionAccepter(ConnectionHub connectionHub)
        {
            _connectionHub = connectionHub;
        }

        protected void Connect(Guid sessionId, Connection connection)
        {
            var connected = _connectionHub.FillBooth(sessionId, connection);
            if (!connected)
                throw new ArgumentException(nameof(sessionId));
        }

        public abstract Task Accept(HttpContext httpContext, Guid sessionId);
    }
}
using System;
using System.Threading.Tasks;
using AgentDeploy.Services;
using AgentDeploy.Services.Models;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.ExternalApi.Websocket
{
    public class WebsocketConnectionAccepter : ConnectionAccepter
    {
        private readonly IOperationContext _operationContext;

        public WebsocketConnectionAccepter(IOperationContext operationContext, ConnectionHub connectionHub) : base(connectionHub)
        {
            _operationContext = operationContext;
        }

        public override Task Accept(HttpContext httpContext, Guid sessionId)
        {
            var websocketConnection = new WebsocketConnection(httpContext, _operationContext);
            Connect(sessionId, websocketConnection);
            return websocketConnection.KeepConnectionOpen();
        }
    }
}
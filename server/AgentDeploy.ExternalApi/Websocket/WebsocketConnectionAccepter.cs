using System;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Services.Websocket;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.ExternalApi.Websocket
{
    public class WebsocketConnectionAccepter : ConnectionAccepter
    {
        private readonly IOperationContext _operationContext;

        public WebsocketConnectionAccepter(IOperationContext operationContext, IConnectionHub connectionHub) : base(connectionHub)
        {
            _operationContext = operationContext;
        }

        public override async Task Accept(HttpContext httpContext, Guid sessionId)
        {
            var websocketConnection = new WebsocketConnection(httpContext, _operationContext);
            await Connect(sessionId, websocketConnection);
            await websocketConnection.KeepConnectionOpen();
        }
    }
}
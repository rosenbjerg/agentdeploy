using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Websocket;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("websocket")]
    public class WebsocketController : ControllerBase
    {
        private readonly ConnectionAccepter _connectionAccepter;

        public WebsocketController(ConnectionAccepter connectionAccepter)
        {
            _connectionAccepter = connectionAccepter;
        }
        
        [HttpGet("connect/{sessionId}")]
        public async Task Connect([FromRoute, Required] Guid sessionId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
                await _connectionAccepter.Accept(HttpContext, sessionId);
            else
                HttpContext.Response.StatusCode = (int) HttpStatusCode.SwitchingProtocols;
            await HttpContext.Response.CompleteAsync();
        }
    }
}
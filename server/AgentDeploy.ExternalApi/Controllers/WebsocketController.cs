using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Websocket;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Services.Websocket;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("websocket")]
    public class WebsocketController : ControllerBase
    {
        private readonly IConnectionAccepter _connectionAccepter;

        public WebsocketController(IConnectionAccepter connectionAccepter)
        {
            _connectionAccepter = connectionAccepter;
        }
        
        [HttpGet("connect/{sessionId}")]
        public async Task Connect([FromRoute, Required] Guid sessionId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    await _connectionAccepter.Accept(HttpContext, sessionId);
                }
                catch (WebsocketSessionNotFoundException)
                {
                    HttpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                }
            }
            else
            {
                HttpContext.Response.StatusCode = (int) HttpStatusCode.SwitchingProtocols;
            }
            await HttpContext.Response.CompleteAsync();
        }
    }
}
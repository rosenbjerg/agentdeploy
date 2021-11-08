using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Services.Websocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("websocket")]
    public class WebsocketController : ControllerBase
    {
        private readonly IConnectionAccepter _connectionAccepter;
        private readonly ILogger<WebsocketController> _logger;

        public WebsocketController(IConnectionAccepter connectionAccepter, ILogger<WebsocketController> logger)
        {
            _connectionAccepter = connectionAccepter;
            _logger = logger;
        }
        
        [HttpGet("connect/{sessionId}")]
        public async Task Connect([FromRoute, Required] Guid sessionId)
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                try
                {
                    _logger.LogDebug("Starting WebSocket session {SessionId}", sessionId);
                    await _connectionAccepter.Accept(HttpContext, sessionId);
                    _logger.LogDebug("WebSocket session {SessionId} ended", sessionId);
                }
                catch (WebsocketSessionNotFoundException)
                {
                    _logger.LogWarning("WebSocket session {SessionId} not found", sessionId);
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
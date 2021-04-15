using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.Services.Websocket
{
    public interface IConnectionAccepter
    {
        Task Accept(HttpContext httpContext, Guid sessionId);
    }
}
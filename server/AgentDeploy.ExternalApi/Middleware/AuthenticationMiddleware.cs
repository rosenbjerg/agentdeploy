using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.ExternalApi.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task Invoke(HttpContext context, OperationContext operationContext, AgentOptions agentOptions, TokenReader tokenReader)
        {
            if (!string.IsNullOrEmpty(operationContext.TokenString))
            {
                var token = await tokenReader.ParseTokenFile(operationContext.TokenString, context.RequestAborted);
                if (token != null)
                {
                    operationContext.Token = token;
                }
            }

            await _next(context);
        }
    }
}
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.Services;
using AgentDeploy.Services.Models;
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

        public async Task Invoke(HttpContext context, OperationContext operationContext, TokenReader tokenReader)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var headerValue))
            {
                var tokenString = headerValue.FirstOrDefault()?.Replace("Token ", string.Empty);
                if (!string.IsNullOrEmpty(tokenString))
                {
                    var token = await tokenReader.ParseTokenFile(tokenString, context.RequestAborted);
                    if (token != null)
                    {
                        operationContext.Token = token;
                        operationContext.TokenString = tokenString;
                        operationContext.OperationCancelled = context.RequestAborted;
                    }
                }
            }

            await _next(context);
        }
    }
}
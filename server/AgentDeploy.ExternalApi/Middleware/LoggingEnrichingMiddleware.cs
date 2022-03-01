using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AgentDeploy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("AgentDeploy.Tests")]
namespace AgentDeploy.ExternalApi.Middleware
{
    public class LoggingEnrichingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingEnrichingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILogger<LoggingEnrichingMiddleware> logger, IOperationContext operationContext)
        {
            var meta = new Dictionary<string, object>
            {
                [nameof(IOperationContext.CorrelationId)] = operationContext.CorrelationId,
                [nameof(IOperationContext.ClientIp)] = operationContext.ClientIp
            };

            if (operationContext.Token != null!)
            {
                meta.Add(nameof(IOperationContext.Token), HideTokenString(operationContext.TokenString));
            }
            
            using (logger.BeginScope(meta))
            {
                await _next(context);
            }
        }
        
        internal static string HideTokenString(string tokenString)
        {
            if (tokenString.Length > 5)
                return $"{tokenString[..2]}{new string('*', tokenString.Length - 4)}{tokenString[^2..]}";
            else if (tokenString.Length > 3)
                return $"{tokenString[..1]}{new string('*', tokenString.Length - 1)}";
            else
                return new string('*', tokenString.Length);
        }
    }
}
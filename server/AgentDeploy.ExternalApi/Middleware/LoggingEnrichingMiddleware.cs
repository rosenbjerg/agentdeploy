using System.Collections.Generic;
using System.Threading.Tasks;
using AgentDeploy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
            using (logger.BeginScope(new Dictionary<string, object>{
                [nameof(IOperationContext.CorrelationId)] = operationContext.CorrelationId
            }))
            {
                await _next(context);
            }
        }
    }
}
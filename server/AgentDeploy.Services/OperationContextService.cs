using System.Linq;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.Services
{
    public interface IOperationContextService
    {
        OperationContext Create();
    }
    public class OperationContextService : IOperationContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AgentOptions _agentOptions;

        public OperationContextService(IHttpContextAccessor httpContextAccessor, AgentOptions agentOptions)
        {
            _httpContextAccessor = httpContextAccessor;
            _agentOptions = agentOptions;
        }

        public OperationContext Create()
        {
            return new OperationContext
            {
                ClientIp = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress,
                TokenString = ExtractTokenString(_httpContextAccessor.HttpContext),
                OperationCancelled = _httpContextAccessor.HttpContext.RequestAborted
            };
        }

        private string ExtractTokenString(HttpContext httpContext)
        {
            if ((!_agentOptions.RequireHttps || httpContext.Request.IsHttps) && httpContext.Request.Headers.TryGetValue("Authorization", out var headerValue))
            {
                var tokenString = headerValue.FirstOrDefault()?.Replace("Token ", string.Empty);
                if (!string.IsNullOrEmpty(tokenString))
                {
                    return tokenString;
                }
            }

            return null!;
        }
    }
}
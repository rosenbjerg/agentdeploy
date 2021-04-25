using System.Linq;
using AgentDeploy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using NetTools;

namespace AgentDeploy.ExternalApi.Filters
{
    public class AuthorizedFilter : IAuthorizationFilter
    {
        private readonly IOperationContext _operationContext;
        private readonly ILogger<AuthorizedFilter> _logger;

        public AuthorizedFilter(IOperationContext operationContext, ILogger<AuthorizedFilter> logger)
        {
            _operationContext = operationContext;
            _logger = logger;
        }
        
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (_operationContext.Token != null!)
            {
                var trusted = _operationContext.Token.TrustedIps == null || 
                              _operationContext.Token.TrustedIps.Any(trustedIpRange => IPAddressRange.Parse(trustedIpRange).Contains(_operationContext.ClientIp));

                if (trusted)
                    return;
                else
                    _logger.LogWarning($"Denying access to token {_operationContext.TokenString} from IP {_operationContext.ClientIp}");
            }
            
            context.Result = new UnauthorizedResult();
        }
    }
}
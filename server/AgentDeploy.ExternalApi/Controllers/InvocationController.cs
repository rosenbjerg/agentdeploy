using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Filters;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("rest")]
    public class InvocationController : ControllerBase
    {
        private readonly IInvocationContextService _invocationContextService;
        private readonly IScriptInvocationService _scriptInvocationService;
        private readonly ILogger<InvocationController> _logger;

        public InvocationController(IInvocationContextService invocationContextService, IScriptInvocationService scriptInvocationService, ILogger<InvocationController> logger)
        {
            _invocationContextService = invocationContextService;
            _scriptInvocationService = scriptInvocationService;
            _logger = logger;
        }
        
        [HttpPost("invoke")]
        [Authorized]
        public async Task<IActionResult> InvokeScript([FromForm]ScriptInvocation scriptInvocation)
        {
            try
            {
                var parsedScriptInvocation = ScriptInvocationParser.Parse(scriptInvocation);
                var executionContext = await _invocationContextService.Build(parsedScriptInvocation);
                if (executionContext == null)
                    return NotFound();

                var result = await _scriptInvocationService.Invoke(executionContext);
                return Ok(result);
            }
            catch (FailedInvocationException e)
            {
                _logger.LogInformation("Script invocation of {ScriptName} failed due to: {FailureMessage}", scriptInvocation.ScriptName, e.Message);
                return BadRequest(new FailedInvocation
                {
                    Title = e.Message,
                    Errors = e.Errors
                });
            }
            catch (ScriptLockedException e)
            {
                _logger.LogInformation("Script invocation of {ScriptName} not possible due to locks", scriptInvocation.ScriptName);
                return StatusCode(423, e.Message);
            }
        }
    }
}
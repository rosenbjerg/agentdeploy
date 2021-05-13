using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Filters;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("rest")]
    public class InvocationController : ControllerBase
    {
        private readonly IInvocationContextService _invocationContextService;
        private readonly IScriptInvocationService _scriptInvocationService;

        public InvocationController(IInvocationContextService invocationContextService, IScriptInvocationService scriptInvocationService)
        {
            _invocationContextService = invocationContextService;
            _scriptInvocationService = scriptInvocationService;
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
                return BadRequest(new FailedInvocation
                {
                    Title = e.Message,
                    Errors = e.Errors
                });
            }
            catch (ScriptLockedException e)
            {
                return StatusCode(423, e.Message);
            }
        }
    }
}
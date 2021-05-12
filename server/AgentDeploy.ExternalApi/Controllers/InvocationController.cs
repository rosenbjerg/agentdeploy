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
            catch (InvalidInvocationArgumentsException e)
            {
                return BadRequest(new FailedInvocation
                {
                    Title = "One or more validation errors occured:",
                    Errors = e.Errors.GroupBy(error => error.Name, error => error.Error).ToDictionary(g => g.Key, g => g.ToArray())
                });
            }
            catch (FilePreprocessingFailedException e)
            {
                return BadRequest(new FailedInvocation
                {
                    Title = $"File preprocessing failed with non-zero exit-code: {e.ExitCode}",
                    Errors = new Dictionary<string, string[]>
                    {
                        { e.Name, e.ErrorOutput.ToArray() }
                    }
                });
            }
            catch (ScriptLockedException e)
            {
                return StatusCode(423, e.Message);
            }
        }
    }
}
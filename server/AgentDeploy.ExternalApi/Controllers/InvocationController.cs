using System;
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
        private readonly InvocationContextService _invocationContextService;
        private readonly ScriptExecutionService _scriptExecutionService;
        private readonly IScriptInvocationParser _scriptInvocationParser;

        public InvocationController(InvocationContextService invocationContextService, ScriptExecutionService scriptExecutionService, IScriptInvocationParser scriptInvocationParser)
        {
            _invocationContextService = invocationContextService;
            _scriptExecutionService = scriptExecutionService;
            _scriptInvocationParser = scriptInvocationParser;
        }
        
        [HttpPost("invoke")]
        [Authorized]
        public async Task<IActionResult> InvokeScript([FromForm]ScriptInvocation scriptInvocation)
        {
            try
            {
                var parsedScriptInvocation = _scriptInvocationParser.Parse(scriptInvocation);
                var executionContext = await _invocationContextService.Build(parsedScriptInvocation);
                if (executionContext == null)
                    return NotFound();

                var result = await _scriptExecutionService.Execute(executionContext);
                return Ok(result);
            }
            catch (InvalidInvocationArgumentsException e)
            {
                return BadRequest(new
                {
                    Message = "One or more validation errors occured",
                    Errors = e.Errors
                });
            }
            catch (ScriptLockedException e)
            {
                return StatusCode(423, e.Message);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
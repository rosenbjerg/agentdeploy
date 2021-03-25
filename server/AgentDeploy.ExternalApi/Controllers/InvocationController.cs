using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Filters;
using AgentDeploy.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("rest")]
    public class InvocationController : ControllerBase
    {
        private readonly CommandReader _commandReader;
        private readonly ExecutionContextService _executionContextService;
        private readonly ScriptExecutionService _scriptExecutionService;

        public InvocationController(CommandReader commandReader, ExecutionContextService executionContextService, ScriptExecutionService scriptExecutionService)
        {
            _commandReader = commandReader;
            _executionContextService = executionContextService;
            _scriptExecutionService = scriptExecutionService;
        }
        
        [HttpPost("invoke")]
        [Authorized]
        public async Task<IActionResult> InvokeCommand([FromForm, Required]string command, IFormCollection form)
        {
            try
            {
                var executionContext = await _executionContextService.Build(command, form);
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
        }
    }
}
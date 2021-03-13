using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AgentDeploy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Controllers
{
    [ApiController]
    [Route("rest")]
    public class InvocationController : ControllerBase
    {
        private readonly CommandSpecParser _commandSpecParser;
        private readonly ArgumentParser _argumentParser;
        private readonly TokenFileParser _tokenFileParser;
        private readonly ScriptExecutionService _scriptExecutionService;

        public InvocationController(CommandSpecParser commandSpecParser, ArgumentParser argumentParser, TokenFileParser tokenFileParser, ScriptExecutionService scriptExecutionService)
        {
            _commandSpecParser = commandSpecParser;
            _argumentParser = argumentParser;
            _tokenFileParser = tokenFileParser;
            _scriptExecutionService = scriptExecutionService;
        }
        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeCommand([FromForm, Required]string token, [FromForm, Required]string command, IFormCollection form)
        {
            var profile = await _tokenFileParser.ParseTokenFile(token, HttpContext.RequestAborted);
            if (profile == null || !profile.AvailableCommands.ContainsKey(command))
                return Unauthorized();
            
            var script = await _commandSpecParser.Load(command, HttpContext.RequestAborted);
            if (script == null)
                return NotFound();

            try
            {
                var executionContext = _argumentParser.Parse(form, script, profile.AvailableCommands[command]!);
                var result = await _scriptExecutionService.Execute(script, executionContext, HttpContext.RequestAborted);
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
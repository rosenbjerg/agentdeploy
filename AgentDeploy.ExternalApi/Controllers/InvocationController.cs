using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AgentDeploy.Services;
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
        private readonly ScriptExecutor _scriptExecutor;

        public InvocationController(CommandSpecParser commandSpecParser, ArgumentParser argumentParser, TokenFileParser tokenFileParser, ScriptExecutor scriptExecutor)
        {
            _commandSpecParser = commandSpecParser;
            _argumentParser = argumentParser;
            _tokenFileParser = tokenFileParser;
            _scriptExecutor = scriptExecutor;
        }
        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeCommand([FromForm, Required]string token, [FromForm, Required]string command, IFormCollection form)
        {
            var profile = await _tokenFileParser.ParseTokenFile(token);
            if (profile == null || !profile.AvailableCommands.ContainsKey(command))
                return Forbid();
            
            var script = await _commandSpecParser.Load(command);
            if (script == null)
                return NotFound();

            try
            {
                var (args, env) = _argumentParser.Parse(form, script.Variables, profile.AvailableCommands[command]!);
                var result = await _scriptExecutor.Execute(script, args, env);
                return Ok(result);
            }
            catch (InvalidInvocationArgumentsException e)
            {
                return BadRequest(e.Errors);
            }
        }
    }
}
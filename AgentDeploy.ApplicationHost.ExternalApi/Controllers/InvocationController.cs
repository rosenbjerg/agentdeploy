using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ApplicationHost.ExternalApi.Controllers
{
    [ApiController]
    [Route("api/command")]
    public class InvocationController : ControllerBase
    {
        [HttpPost("invoke")]
        public async Task<IActionResult> InvokeCommand(IFormCollection commandInvocationForm)
        {

            var enviromnentVariables = commandInvocationForm.Where(e => e.Key == "environment").ToList();
            var variables = commandInvocationForm.Where(e => e.Key == "variable").ToList();
            var secretVariables = commandInvocationForm.Where(e => e.Key == "secretVariable").ToList();
            var arguments = commandInvocationForm.Where(e => e.Key == "secretVariable").ToList();
            
            foreach (var argument in commandInvocationForm)
            {
                Console.WriteLine(argument);
            }

            return Ok();
        }
    }
}
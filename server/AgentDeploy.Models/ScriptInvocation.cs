using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.Models
{
    public class ScriptInvocation
    {
        [Required, FromForm(Name = "scriptName")]
        public string ScriptName { get; set; } = null!;

        [FromForm(Name = "websocket-session-id")]
        public Guid? WebsocketSessionId { get; set; }
        
        [FromForm(Name = "variables")]
        public string[] Variables { get; set; } = Array.Empty<string>();
        
        [FromForm(Name = "secretVariables")]
        public string[] SecretVariables { get; set; } = Array.Empty<string>();
        
        [FromForm(Name = "environmentVariables")]
        public string[] EnvironmentVariables { get; set; } = Array.Empty<string>();
        
        [FromForm(Name = "files")]
        public IFormFile[] Files { get; set; } = Array.Empty<IFormFile>();
    }
}
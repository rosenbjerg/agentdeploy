using System;

namespace AgentDeploy.Services.Models
{
    public class WebsocketBoothNotFoundException : Exception
    {
        public WebsocketBoothNotFoundException(string msg) : base(msg)
        {
            
        }
    }
}
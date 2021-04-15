using System;

namespace AgentDeploy.Models.Exceptions
{
    public class WebsocketSessionNotFoundException : Exception
    {
        public WebsocketSessionNotFoundException(string msg) : base(msg)
        {
            
        }
    }
}
using System;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public class WebsocketSessionNotFoundException : Exception
    {
        public WebsocketSessionNotFoundException(string msg) : base(msg)
        {
            
        }
    }
}
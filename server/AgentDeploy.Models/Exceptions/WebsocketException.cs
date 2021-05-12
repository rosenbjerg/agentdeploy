using System;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public class WebsocketException : Exception
    {
        public WebsocketException(string msg) : base(msg)
        {
        }
    }
}
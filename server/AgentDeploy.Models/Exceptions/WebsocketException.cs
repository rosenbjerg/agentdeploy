using System;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class WebsocketException : Exception
    {
        public WebsocketException(string msg) : base(msg)
        {
        }
    }
}
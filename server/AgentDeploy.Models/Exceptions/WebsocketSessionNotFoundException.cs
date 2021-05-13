using System;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class WebsocketSessionNotFoundException : Exception
    {
        public WebsocketSessionNotFoundException(string msg) : base(msg)
        {
        }
    }
}
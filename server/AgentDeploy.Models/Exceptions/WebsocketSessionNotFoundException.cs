using System;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class WebsocketSessionNotFoundException : WebsocketException
    {
        public WebsocketSessionNotFoundException(string msg) : base(msg)
        {
        }
        
        private WebsocketSessionNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
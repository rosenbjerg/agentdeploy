using System;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public class WebsocketException : Exception
    {
        public WebsocketException(string msg) : base(msg)
        {
        }
        
        protected WebsocketException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class InvalidScriptFileException : FailedInvocationException
    {
        public InvalidScriptFileException(Dictionary<string, string[]> errors) 
            : base("Errors found in Script", errors)
        {
        }
        
        private InvalidScriptFileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
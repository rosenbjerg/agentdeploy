using System;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class ScriptLockedException : Exception
    {
        public ScriptLockedException(string scriptName) : base($"The script '{scriptName}' is currently locked. Try again later")
        {
        }
        
        private ScriptLockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
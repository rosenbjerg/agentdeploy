using System;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public class ScriptLockedException : Exception
    {
        public ScriptLockedException(string scriptName) : base($"The script '{scriptName}' is currently locked. Try again later")
        {
        }
    }
}
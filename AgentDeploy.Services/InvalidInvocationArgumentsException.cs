using System;
using System.Collections.Generic;

namespace AgentDeploy.Application.Parser
{
    public class InvalidInvocationArgumentsException : Exception
    {
        public List<InvocationArgumentError> Errors { get; }

        public InvalidInvocationArgumentsException(List<InvocationArgumentError> errors)
        {
            Errors = errors;
        }
    }
}
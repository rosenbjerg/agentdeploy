using System;
using System.Collections.Generic;
using AgentDeploy.Models;

namespace AgentDeploy.Services
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
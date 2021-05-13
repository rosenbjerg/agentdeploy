using System;
using System.Collections.Generic;
using System.Linq;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class FailedInvocationValidationException : FailedInvocationException
    {
        public FailedInvocationValidationException(IEnumerable<InvocationArgumentError> errors) 
            : base("One or more validation errors occured", errors.GroupBy(error => error.Name, error => error.Error).ToDictionary(g => g.Key, g => g.ToArray()))
        {
        }
    }
}
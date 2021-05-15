using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class FailedInvocationValidationException : FailedInvocationException
    {
        public FailedInvocationValidationException(IEnumerable<InvocationArgumentError> errors) 
            : base("One or more validation errors occured", errors.GroupBy(error => error.Name, error => error.Error).ToDictionary(g => g.Key, g => g.ToArray()))
        {
        }
        
        private FailedInvocationValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
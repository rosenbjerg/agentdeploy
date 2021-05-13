using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class InvalidInvocationArgumentsException : Exception
    {
        public List<InvocationArgumentError> Errors { get; }

        public InvalidInvocationArgumentsException(List<InvocationArgumentError> errors)
        {
            Errors = errors;
        }

        private InvalidInvocationArgumentsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Errors = (List<InvocationArgumentError>) info.GetValue($"{nameof(InvalidInvocationArgumentsException)}.{nameof(Errors)}", typeof(List<InvocationArgumentError>))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue($"{nameof(InvalidInvocationArgumentsException)}.{nameof(Errors)}", Errors);
        }
    }
}
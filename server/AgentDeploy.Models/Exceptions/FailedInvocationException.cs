using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public abstract class FailedInvocationException : Exception
    {
        public Dictionary<string, string[]> Errors { get; }

        protected FailedInvocationException(string message, Dictionary<string, string[]> errors)
            : base(message)
        {
            Errors = errors;
        }

        private FailedInvocationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Errors = (Dictionary<string, string[]>) info.GetValue($"{nameof(FailedInvocationValidationException)}.{nameof(Errors)}", typeof(Dictionary<string, string[]>))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue($"{nameof(FailedInvocationValidationException)}.{nameof(Errors)}", Errors);
        }
    }
}
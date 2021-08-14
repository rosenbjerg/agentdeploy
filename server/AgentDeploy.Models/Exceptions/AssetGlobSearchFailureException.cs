using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class AssetGlobSearchFailureException : FailedInvocationException
    {
        public AssetGlobSearchFailureException(string[] missingFiles) 
            : base("One or more of the asset globs did not match any files", new Dictionary<string, string[]>{ {"Missing files", missingFiles} })
        {
        }
        
        private AssetGlobSearchFailureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
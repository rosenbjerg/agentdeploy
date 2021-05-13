using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class FilePreprocessingFailedException : FailedInvocationException
    {
        public FilePreprocessingFailedException(string name, int exitCode, IReadOnlyList<string> errorOutput)
            : base($"File preprocessing failed with non-zero exit-code: {exitCode}", new Dictionary<string, string[]> {{ name, errorOutput.ToArray() }})
        {
        }
        
        private FilePreprocessingFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
using System;
using System.Collections.Generic;

namespace AgentDeploy.Models.Exceptions
{
    public class FilePreprocessingFailedException : Exception
    {
        public string Name { get; }
        public int ExitCode { get; }
        public IReadOnlyList<string> ErrorOutput { get; }

        public FilePreprocessingFailedException(string name, int exitCode, IReadOnlyList<string> errorOutput)
        {
            Name = name;
            ExitCode = exitCode;
            ErrorOutput = errorOutput;
        }
    }
}
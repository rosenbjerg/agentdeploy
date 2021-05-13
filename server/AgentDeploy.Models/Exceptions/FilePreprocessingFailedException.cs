using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AgentDeploy.Models.Exceptions
{
    [Serializable]
    public sealed class FilePreprocessingFailedException : Exception
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

        private FilePreprocessingFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Name = (string) info.GetValue($"{nameof(FilePreprocessingFailedException)}.{nameof(Name)}", typeof(string))!;
            ExitCode = (int) info.GetValue($"{nameof(FilePreprocessingFailedException)}.{nameof(ExitCode)}", typeof(int))!;
            ErrorOutput = (IReadOnlyList<string>) info.GetValue($"{nameof(FilePreprocessingFailedException)}.{nameof(ErrorOutput)}", typeof(IReadOnlyList<string>))!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue($"{nameof(FilePreprocessingFailedException)}.{nameof(Name)}", Name);
            info.AddValue($"{nameof(FilePreprocessingFailedException)}.{nameof(ExitCode)}", ExitCode);
            info.AddValue($"{nameof(FilePreprocessingFailedException)}.{nameof(ErrorOutput)}", ErrorOutput);
        }
    }
}
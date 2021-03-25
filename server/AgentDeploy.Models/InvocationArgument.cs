using System;
using System.IO;

namespace AgentDeploy.Models
{
    public record InvocationArgument(string Name, ArgumentType Type, string Value, bool Secret);
    public record InvocationFile(string Name, string FileName, Func<Stream> OpenRead);
    public record InvocationArgumentError(string Name, string Error);
    public record RawInvocationArgument(string Name, string Value, bool Secret);
}
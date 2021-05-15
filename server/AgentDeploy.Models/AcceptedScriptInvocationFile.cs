using System;
using System.IO;

namespace AgentDeploy.Models
{
    public record AcceptedScriptInvocationFile(string Name, string FileName, string? Preprocessing, Func<Stream> OpenRead);
}
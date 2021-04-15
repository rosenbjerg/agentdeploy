using System;
using System.IO;

namespace AgentDeploy.Models
{
    public record AcceptedScriptInvocationFile(string Name, string FileName, Func<Stream> OpenRead);
}
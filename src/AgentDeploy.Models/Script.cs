using System;
using System.Collections.Generic;
using System.IO;

namespace AgentDeploy.Models
{
    public class Script
    {
        public Dictionary<string, ScriptArgument> Variables { get; set; } = new();
        public Dictionary<string, ScriptFileArgument> Files { get; set; } = new();
        public string Command { get; set; } = null!;
        public bool ShowCommand { get; set; } = false;
        public bool ShowOutput { get; set; } = true;
    }
    public record InvocationArgument(string Name, ArgumentType Type, string Value, bool Secret);
    public record InvocationFile(string Name, string FileName, Func<Stream> OpenRead);
    public record InvocationArgumentError(string Name, string Error);
    public record RawInvocationArgument(string Name, string Value, bool Secret);
    
    public record ScriptExecutionContext(List<InvocationArgument> Arguments, InvocationFile[] Files,
        string[] EnvironmentVariables, SecureShellOptions? SecureShellOptions);
}
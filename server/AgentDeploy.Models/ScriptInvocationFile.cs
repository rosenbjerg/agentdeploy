using System;
using System.IO;

namespace AgentDeploy.Models
{
    public class ScriptInvocationFile
    {
        public string Key { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public Func<Stream> Read { get; set; } = null!;
    }
}
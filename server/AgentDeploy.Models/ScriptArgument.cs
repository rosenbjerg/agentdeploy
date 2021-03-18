namespace AgentDeploy.Models
{
    public class ScriptArgument
    {
        public ArgumentType Type { get; set; }
        public string? DefaultValue { get; set; }
        public string? Regex { get; set; }
        public bool Secret { get; set; }
    }
    public class ScriptFileArgument
    {
        public long MinSize { get; set; } = 0;
        public long MaxSize { get; set; } = long.MaxValue;
        public string[]? AcceptedExtensions { get; set; }
    }
}
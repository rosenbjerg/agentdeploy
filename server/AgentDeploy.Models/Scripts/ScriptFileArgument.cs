namespace AgentDeploy.Models.Scripts
{
    public class ScriptFileArgument
    {
        public long MinSize { get; set; } = 0;
        public long MaxSize { get; set; } = long.MaxValue;
        public string[]? AcceptedExtensions { get; set; }
    }
}
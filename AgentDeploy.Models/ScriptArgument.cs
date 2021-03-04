namespace AgentDeploy.Services.Models
{
    public class ScriptArgument
    {
        public ArgumentType Type { get; set; }
        public string? DefaultValue { get; set; }
        public string? Regex { get; set; }
    }
}
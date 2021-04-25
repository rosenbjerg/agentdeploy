namespace AgentDeploy.Models.Scripts
{
    public class ScriptArgumentDefinition
    {
        public ScriptArgumentType Type { get; set; } = ScriptArgumentType.String;
        public string? DefaultValue { get; set; }
        public string? Regex { get; set; }
        public bool Secret { get; set; }
    }
}
namespace AgentDeploy.Models.Scripts
{
    public class ScriptVariableDefinition
    {
        public ScriptArgumentType Type { get; set; } = ScriptArgumentType.String;
        public string? DefaultValue { get; set; }
        public string? Regex { get; set; }
        public bool Secret { get; set; }
    }
}
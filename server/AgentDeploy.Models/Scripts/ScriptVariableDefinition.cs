namespace AgentDeploy.Models.Scripts
{
    public class ScriptVariableDefinition
    {
        public ScriptArgumentType Type { get; init; } = ScriptArgumentType.String;
        public string? DefaultValue { get; init; }
        public string? Regex { get; init; }
        public bool Secret { get; init; }
    }
}
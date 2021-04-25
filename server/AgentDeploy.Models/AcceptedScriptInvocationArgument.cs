using AgentDeploy.Models.Scripts;

namespace AgentDeploy.Models
{
    public record AcceptedScriptInvocationArgument(string Name, ScriptArgumentType Type, string Value, bool Secret);
}
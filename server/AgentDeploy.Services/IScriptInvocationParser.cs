using AgentDeploy.Models;

namespace AgentDeploy.Services
{
    public interface IScriptInvocationParser
    {
        ParsedScriptInvocation Parse(ScriptInvocation scriptInvocation);
    }
}
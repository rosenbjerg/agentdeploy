using AgentDeploy.Models;

namespace AgentDeploy.Services.ScriptExecutors
{
    public interface IScriptExecutorFactory
    {
        IScriptExecutor Build(ScriptInvocationContext invocationContext);
    }
}
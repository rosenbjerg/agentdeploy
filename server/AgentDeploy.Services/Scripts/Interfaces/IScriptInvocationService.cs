using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptInvocationService
    {
        Task<ExecutionResult> Invoke(ScriptInvocationContext invocationContext);
    }
}
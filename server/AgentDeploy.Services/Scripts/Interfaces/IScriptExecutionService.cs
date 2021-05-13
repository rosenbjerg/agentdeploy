using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptExecutionService
    {
        Task<ExecutionResult> Execute(ScriptInvocationContext invocationContext, string directory, CancellationToken cancellationToken);
    }
}
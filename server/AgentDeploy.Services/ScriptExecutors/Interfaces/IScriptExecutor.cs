using System;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.ScriptExecutors
{
    public interface IScriptExecutor
    {
        Task<int> Execute(ScriptInvocationContext invocationContext, string directory, Action<ProcessOutput> onOutput);
    }
}
using System;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services
{
    public interface IScriptExecutor
    {
        Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<string, bool> onOutput);
    }
}
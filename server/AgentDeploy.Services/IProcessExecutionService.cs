using System;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public interface IProcessExecutionService
    {
        Task<ProcessExecutionResult> Invoke(string executionOptionsShell, string fileArgument, Action<string, bool> onOutput);
    }
}
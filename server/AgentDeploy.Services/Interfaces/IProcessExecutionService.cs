using System;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public interface IProcessExecutionService
    {
        Task<ProcessExecutionResult> Invoke(string executable, string arguments, Action<string, bool>? onOutput, string workingDir = "/");
    }
}
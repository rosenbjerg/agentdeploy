using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Services
{
    public sealed class ProcessExecutionService : IProcessExecutionService
    {
        public async Task<ProcessExecutionResult> Invoke(string executable, string arguments,
            Action<string, bool>? onOutput, string workingDir = "/", CancellationToken cancellationToken = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDir
            };
            var result = await Instance.FinishAsync(startInfo, cancellationToken, (_, s) => onOutput?.Invoke(s, false), (_, s) => onOutput?.Invoke(s, true));
            return new ProcessExecutionResult(result.ExitCode, result.OutputData, result.ErrorData);
        }
    }
}
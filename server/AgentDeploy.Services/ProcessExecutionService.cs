using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Services
{
    public sealed class ProcessExecutionService : IProcessExecutionService
    {
        public async Task<ProcessExecutionResult> Invoke(string executable, string arguments,
            Action<string, bool>? onOutput, string workingDir = "/")
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDir
            };
            var result = await Instance.FinishAsync(startInfo, (_, tuple) => onOutput?.Invoke(tuple.Data, tuple.Type == DataType.Error));
            return new ProcessExecutionResult(result.exitCode, result.instance.OutputData, result.instance.ErrorData);
        }
    }
}
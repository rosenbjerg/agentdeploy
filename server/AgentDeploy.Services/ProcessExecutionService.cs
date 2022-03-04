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
            var instance = new Instance(startInfo);
            instance.DataReceived += (_, args) => onOutput?.Invoke(args.Data, args.Type == DataType.Error);

            cancellationToken.Register(() => instance.Started = false);
            var exitCode = await instance.FinishedRunning();
            
            return new ProcessExecutionResult(exitCode, instance.OutputData, instance.ErrorData);
        }
    }
}
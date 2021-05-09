using System;
using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Services
{
    public sealed class ProcessExecutionService : IProcessExecutionService
    {
        public async Task<ProcessExecutionResult> Invoke(string executionOptionsShell, string fileArgument,
            Action<string, bool> onOutput)
        {
            var result = await Instance.FinishAsync(executionOptionsShell, fileArgument, (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error));
            return new ProcessExecutionResult(result.exitCode, result.instance.OutputData, result.instance.ErrorData);
        }
    }
}
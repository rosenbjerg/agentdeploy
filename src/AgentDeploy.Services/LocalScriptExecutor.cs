using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using Instances;

namespace AgentDeploy.Services
{
    public class LocalScriptExecutor : IScriptExecutor
    {
        private readonly ExecutionOptions _executionOptions;

        public LocalScriptExecutor(ExecutionOptions executionOptions)
        {
            _executionOptions = executionOptions;
        }

        public async Task<int> Execute(ScriptExecutionContext executionContext, string directory, Action<string, bool> onOutput, CancellationToken cancellationToken)
        {
            var scriptFilePath = Path.Combine(directory, "script.sh");
            var instance = new Instance(_executionOptions.Shell, scriptFilePath);
            instance.DataReceived += (_, tuple) => onOutput(tuple.Data, tuple.Type == DataType.Error);
            cancellationToken.ThrowIfCancellationRequested();
            return await instance.FinishedRunning();
        }
    }
}
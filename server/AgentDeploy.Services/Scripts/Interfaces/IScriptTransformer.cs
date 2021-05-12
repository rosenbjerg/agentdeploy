using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptTransformer
    {
        Task<string> PrepareScriptFile(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken);
        string BuildScriptPath(string directory);
        string BuildScriptArgument(string scriptFilePath);
    }
}
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptInvocationFileService
    {
        Task DownloadFiles(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken);
    }
}
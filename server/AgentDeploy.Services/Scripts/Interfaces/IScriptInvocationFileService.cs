using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Scripts;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptInvocationFileService
    {
        Task DownloadFiles(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken);

        Task CopyAssets(Script script, string directory, CancellationToken cancellationToken);
    }
}
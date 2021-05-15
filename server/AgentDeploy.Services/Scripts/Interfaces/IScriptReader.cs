using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Scripts;

namespace AgentDeploy.Services.Scripts
{
    public interface IScriptReader
    {
        Task<Script?> Load(string scriptName, CancellationToken cancellationToken);
    }
}
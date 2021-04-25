using System.Threading.Tasks;
using AgentDeploy.Models.Scripts;

namespace AgentDeploy.Services.Locking
{
    public interface IScriptInvocationLockService
    {
        Task<IScriptInvocationLock> Lock(Script script, string token);
    }
}
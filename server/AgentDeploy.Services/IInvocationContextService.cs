using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services
{
    public interface IInvocationContextService
    {
        Task<ScriptInvocationContext?> Build(ParsedScriptInvocation scriptInvocation);
    }
}
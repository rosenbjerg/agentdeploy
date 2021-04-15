using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Services
{
    public interface ITokenReader
    {
        Task<Token?> ParseTokenFile(string token, CancellationToken cancellationToken = default);
    }
}
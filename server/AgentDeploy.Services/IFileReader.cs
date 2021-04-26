using System.Threading;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public interface IFileReader
    {
        Task<string?> ReadAsync(string? filePath, CancellationToken cancellationToken);
        string? FindFile(string directory, string filename, params string[] extensions);
    }
}
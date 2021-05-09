using System.Threading;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public interface IFileService
    {
        Task<string?> ReadAsync(string? filePath, CancellationToken cancellationToken);
        string? FindFile(string directory, string filename, params string[] extensions);
        Task WriteText(string filePath, string content, CancellationToken cancellationToken);
    }
}
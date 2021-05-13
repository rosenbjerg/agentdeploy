using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public interface IFileService
    {
        Task<string?> ReadAsync(string? filePath, CancellationToken cancellationToken);
        string? FindFile(string directoryPath, string fileName, params string[] extensions);
        Task WriteText(string filePath, string content, CancellationToken cancellationToken);
        Task Write(Stream inputStream, string filePath, CancellationToken cancellationToken);
        void CreateDirectory(string directoryPath);
        void DeleteDirectory(string directoryPath, bool recursive);
        bool FileExists(string filePath);
        void DeleteFile(string filePath);
    }
}
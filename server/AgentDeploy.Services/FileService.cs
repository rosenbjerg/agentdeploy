using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;

namespace AgentDeploy.Services
{
    public sealed class FileService : IFileService
    {
        private readonly ExecutionOptions _executionOptions;

        public FileService(ExecutionOptions executionOptions)
        {
            _executionOptions = executionOptions;
        }

        public async Task<string?> ReadAsync(string? filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }

        public string? FindFile(string directory, string filename, params string[] extensions)
        {
            return extensions
                .Select(extension => $"{directory}{_executionOptions.DirectorySeparatorChar}{filename}.{extension}")
                .FirstOrDefault(File.Exists);
        }

        public async Task WriteText(string filePath, string content, CancellationToken cancellationToken)
        {
            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }
    }
}
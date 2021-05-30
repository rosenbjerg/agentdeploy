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

        public string? FindFile(string directoryPath, string fileName, params string[] extensions)
        {
            return extensions
                .Select(extension => PathUtils.Combine(_executionOptions.DirectorySeparatorChar, directoryPath, $"{fileName}.{extension}"))
                .FirstOrDefault(File.Exists);
        }

        public async Task WriteTextAsync(string filePath, string content, CancellationToken cancellationToken) =>
            await File.WriteAllTextAsync(filePath, content, cancellationToken);

        public async Task WriteAsync(Stream inputStream, string filePath, CancellationToken cancellationToken)
        {
            await using var fileStream = File.Create(filePath);
            await inputStream.CopyToAsync(fileStream, cancellationToken);
        }

        public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

        public void DeleteDirectory(string directoryPath, bool recursive) => Directory.Delete(directoryPath, recursive);

        public bool FileExists(string filePath) => File.Exists(filePath);

        public void DeleteFile(string filePath) => File.Delete(filePath);
        
        public async Task CopyFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken)
        {
            await using var inputStream = File.OpenRead(sourcePath);
            await WriteAsync(inputStream, destinationPath, cancellationToken);
        }
    }
}
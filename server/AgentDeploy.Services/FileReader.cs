using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public class FileReader : IFileReader
    {
        public async Task<string?> ReadAsync(string? filePath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }

        public string? FindFile(string directory, string filename, params string[] extensions)
        {
            return extensions
                .Select(extension => Path.Combine(directory, $"{filename}.{extension}"))
                .FirstOrDefault(File.Exists);
        }
    }
}
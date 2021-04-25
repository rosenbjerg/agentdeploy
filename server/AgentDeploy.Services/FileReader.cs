using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AgentDeploy.Services
{
    public class FileReader : IFileReader
    {
        public async Task<string?> ReadAsync(string filePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(filePath))
                return null;

            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
    }
}
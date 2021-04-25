using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Tests.E2E
{
    public static class E2ETestUtils
    {
        private static readonly string AgentdClientPath;

        static E2ETestUtils()
        {
            var parts = Path.GetFullPath("./").Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var index = Array.LastIndexOf(parts, "AgentDeploy");
            var rootParts = parts.Take(index + 1);
            var root = string.Join(Path.DirectorySeparatorChar, rootParts);
            var clientDirectory = Path.Combine(root, "client", "src");
            if (Directory.Exists(clientDirectory))
                AgentdClientPath = clientDirectory;
            else
            {
                throw new DirectoryNotFoundException(clientDirectory);
            }
        }
        
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments)
        {
            return await Instance.FinishAsync("node", AgentdClientPath + " " +arguments);
        }
    }
}
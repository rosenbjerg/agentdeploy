using System;
using System.IO;
using System.Linq;
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
            var rootParts = parts.TakeWhile(part => part != "server");
            var root = string.Join(Path.DirectorySeparatorChar, rootParts);

            var clientDirectory = Path.Combine(root, "client", "src");
            if (Directory.Exists(clientDirectory))
                AgentdClientPath = clientDirectory;
            else
            {
                var repoDir = Directory.EnumerateDirectories("./", "AgentDeploy", SearchOption.AllDirectories).Single();
                AgentdClientPath = Path.Combine(repoDir, "client", "src");
            }
        }
        
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments)
        {
            return await Instance.FinishAsync("node", AgentdClientPath + " " +arguments);
        }
    }
}
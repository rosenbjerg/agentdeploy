using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Tests.E2E
{
    public static class E2ETestUtils
    {
        private static readonly string AgentdClientPath;

        static E2ETestUtils()
        {
            var path = Path.GetFullPath("./");
            var rootDir = path.Substring(0, path.LastIndexOf("server", StringComparison.InvariantCulture) - 1);
            AgentdClientPath = Path.Combine(rootDir, "client", "src");
            if (!File.Exists(Path.Combine(AgentdClientPath, "index.js")))
                throw new FileNotFoundException(Path.Combine(AgentdClientPath, "index.js"));
        }
        
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments, CancellationToken cancellationToken = default)
        {
            var instance = new Instance("node", $"{AgentdClientPath} {arguments}");
            cancellationToken.Register(() => instance.Started = false);
            var exitCode = await instance.FinishedRunning();
            return (exitCode, instance);
        }
    }
}
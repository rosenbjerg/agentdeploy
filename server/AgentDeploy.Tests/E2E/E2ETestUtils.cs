using System;
using System.IO;
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
            var rootDir = path.Substring(0, path.IndexOf("AgentDeploy", StringComparison.InvariantCulture) - 1);
            AgentdClientPath = Path.Combine(rootDir, "AgentDeploy", "client", "src");
            if (!File.Exists(Path.Combine(AgentdClientPath, "index.js")))
                throw new FileNotFoundException(Path.Combine(AgentdClientPath, "index.js"));
        }
        
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments)
        {
            return await Instance.FinishAsync("node", AgentdClientPath + " " +arguments);
        }
    }
}
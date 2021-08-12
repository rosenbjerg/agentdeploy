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
            var rootDir = path.Substring(0, path.LastIndexOf("server", StringComparison.InvariantCulture) - 1);
            AgentdClientPath = Path.Combine(rootDir, "client", "src");
            if (!File.Exists(Path.Combine(AgentdClientPath, "index.js")))
                throw new FileNotFoundException(Path.Combine(AgentdClientPath, "index.js"));
        }
        
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments)
        {
            return await Instance.FinishAsync("node", AgentdClientPath + " " +arguments);
        }

        private static string _containerName = "ssh-target-dummy";
        public static async Task SshTargetDummyStart()
        {
            var path = Path.GetFullPath("./");
            Console.WriteLine(path);
            var (buildExitCode, buildInstance) = await Instance.FinishAsync("docker", $"build -t {_containerName} E2E/Files");
            if (buildExitCode != 0)
                throw new Exception($"Unable to build {_containerName} image from E2E/Files: {string.Join("\n", buildInstance.ErrorData)}");
            
            var (startExitCode, startInstance) = await Instance.FinishAsync("docker", $"run --rm -d -p 127.0.0.1:222:22/tcp --name={_containerName} {_containerName}");
            if (startExitCode != 0)
                throw new Exception($"Unable to start {_containerName}: {string.Join("\n", startInstance.ErrorData)}");

            await Task.Delay(1000);
        }

        public static async Task SshTargetDummyStop()
        {
            var (stopExitCode, stopInstance) = await Instance.FinishAsync("docker", $"stop {_containerName}");
            if (stopExitCode != 0)
                throw new Exception($"Unable to stop {_containerName}: {string.Join("\n", stopInstance.ErrorData)}");
        }
    }
}
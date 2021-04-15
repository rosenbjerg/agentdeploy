using System.Threading.Tasks;
using Instances;

namespace AgentDeploy.Tests.E2E
{
    public static class E2ETestUtils
    {
        private const string AgentdClientPath = "C:\\Users\\malte\\Documents\\GitHub\\AgentDeploy\\client\\dist\\agentd-client-win.exe";
        public static Instance AgentdClient(string arguments)
        {
            return new Instance(AgentdClientPath, arguments);
        }
        public static async Task<(int exitCode, Instance instance)> ClientOutput(string arguments)
        {
            return await Instance.FinishAsync(AgentdClientPath, arguments);
        }
    }
}
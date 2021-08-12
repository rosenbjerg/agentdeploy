using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AgentDeploy.Models.Tokens;
using Instances;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E")]
    public class TestsWithServerAndExplicitSshKeyTarget : TestsWithServer
    {
        private const string PrivateKeyPath = "E2E/Files/id_rsa";
        public TestsWithServerAndExplicitSshKeyTarget() : base(new SecureShellOptions
        {
            Address = "127.0.0.1",
            Username = "root",
            PrivateKeyPath = Path.GetFullPath(PrivateKeyPath),
            Port = 5022,
        })
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set permissions on id_rsa
                Instance.FinishAsync($"chmod 600 {SecureShellOptions!.PrivateKeyPath}");
            }
        }

        protected override async Task Setup()
        {
            await E2ETestUtils.SshTargetDummyStart();
        }

        protected override async Task Teardown()
        {
            await E2ETestUtils.SshTargetDummyStop();
        }
    }
}
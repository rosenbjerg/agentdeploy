using System.IO;
using System.Threading.Tasks;
using AgentDeploy.Models.Tokens;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E")]
    public class TestsWithServerAndExplicitSshKeyTarget : TestsWithServer
    {
        public TestsWithServerAndExplicitSshKeyTarget() : base(new SecureShellOptions
        {
            Address = "127.0.0.1",
            Username = "root",
            PrivateKeyPath = Path.GetFullPath("E2E/Files/id_rsa"),
            Port = 222,
        })
        {
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
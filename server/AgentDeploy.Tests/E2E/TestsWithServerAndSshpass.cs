using AgentDeploy.Models.Tokens;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E-sshpass")]
    public class TestsWithServerAndSshpass : TestsWithServer
    {
        public TestsWithServerAndSshpass() : base(new SecureShellOptions
        {
            Address = "ssh-target",
            Username = "root",
            Password = "root"
        })
        {
        }
    }
}
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E-local")]
    public class TestsWithServerAndLocalTarget : TestsWithServer
    {
        public TestsWithServerAndLocalTarget() : base(null)
        {
        }
    }
}
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E-Local")]
    public class TestsWithServerAndLocalTarget : TestsWithServer
    {
        public TestsWithServerAndLocalTarget() : base(null)
        {
        }
    }
}
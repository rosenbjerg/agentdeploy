using AgentDeploy.ExternalApi.Middleware;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class LoggingEnrichmentTests
    {
        [TestCase("test", "t***")]
        [TestCase("1", "*")]
        [TestCase("teeest", "te**st")]
        [TestCase("mysecrettoken", "my*********en")]
        public void VerifyHidingTokenForLogs(string input, string expected)
        {
            var actual = LoggingEnrichingMiddleware.HideTokenString(input);
            Assert.AreEqual(expected, actual);
        }
    }
}
using System.Threading.Tasks;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E")]
    public class TestsWithoutServer
    {
        [Test]
        public async Task HelpArgument_Succeeds()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("--help");
            Assert.Zero(exitCode);
            Assert.AreEqual("Usage: agentd client [options] [command]", instance.OutputData[0]);
        }
        
        [Test]
        public async Task InvalidArgument_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("nope");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: unknown command 'nope'. See 'agentd client --help'.", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingCommand_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: missing required argument 'script'", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingServerUrl_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: missing required argument 'serverUrl'", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task InvalidServerUrl_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test invalid-url -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: Only absolute URLs are supported", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task InvalidOption_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke -t test -xxx test http://localhost:4999");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: unknown option '-xxx'", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_MissingToken_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:4999");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: token must be provided by placing a file containing the token at the path ./agentd.token or by using the token argument (-t)", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_NoServer_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:4999 -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: request to http://localhost:4999/rest/invoke failed, reason: connect ECONNREFUSED 127.0.0.1:4999", instance.ErrorData[0]);
        }
    }
}
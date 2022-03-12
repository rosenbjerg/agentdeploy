using System.Threading.Tasks;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E-notarget")]
    public class TestsWithoutServer
    {
        [Test]
        public async Task HelpArgument_Succeeds()
        {
            var result = await E2ETestUtils.ClientOutput("--help");
            Assert.Zero(result.ExitCode);
            Assert.AreEqual("Usage: agentd-client [options] [command]", result.OutputData[0]);
        }
        
        [Test]
        public async Task InvalidArgument_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("nope");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("error: unknown command 'nope'. See 'agentd-client --help'.", result.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingScriptName_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("error: missing required argument 'scriptName'", result.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingServerUrl_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke test");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("error: missing required argument 'serverUrl'", result.ErrorData[0]);
        }
        
        [Test]
        public async Task InvalidServerUrl_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke test invalid-url -t test");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("Only absolute URLs are supported", result.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingToken_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:4999");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("The token must either be provided by placing a file containing the token at the path ./agentd.token or by using the token argument (-t)", result.ErrorData[0]);
        }
        
        [Test]
        public async Task InvalidOption_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke -t test -xxx test http://localhost:4999");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("error: unknown option '-xxx'", result.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_MissingToken_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:4999");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("The token must either be provided by placing a file containing the token at the path ./agentd.token or by using the token argument (-t)", result.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_NoServer_Fails()
        {
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:4999 -t test");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("request to http://localhost:4999/rest/invoke failed, reason: connect ECONNREFUSED 127.0.0.1:4999", result.ErrorData[0]);
        }
    }
}
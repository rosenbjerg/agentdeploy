using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services;
using AgentDeploy.Services.Script;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    [Category("E2E")]
    public class TestsWithServer
    {
        private IHost _host;

        [OneTimeSetUp]
        public async Task StartServer()
        {
            var server = Program.CreateHostBuilder<TestApiStartup>(Array.Empty<string>());
            _host = await server.StartAsync();
        }

        [OneTimeTearDown]
        public async Task StopServer()
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        [TearDown]
        public void Reset()
        {
            var tokenReaderMock = _host.Services.GetRequiredService<Mock<ITokenReader>>();
            var scriptReaderMock = _host.Services.GetRequiredService<Mock<IScriptReader>>();
            tokenReaderMock.Reset();
            scriptReaderMock.Reset();
        }
        
        [Test]
        public async Task ValidServerUrl_InvalidToken_Fails()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke script http://localhost:5000 -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: ", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_ValidToken_MissingCommanddd_()
        {
            var tokenReaderMock = _host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { });
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke echo http://localhost:5000 -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: script test not found", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_ValidToken_MissingCommand_()
        {
            var tokenReaderMock = _host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScript = new Dictionary<string, ScriptAccessDeclaration>() });
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke echo http://localhost:5000 -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: script test not found", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ValidServerUrl_ValidToken_MissingCommandd_()
        {
            var scriptReaderMock = _host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test")).ReturnsAsync(new Script { Command = "echo test"});
            var tokenReaderMock = _host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test_1", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke echo http://localhost:5000 -t test");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("error: script test not found", instance.ErrorData[0]);
        }
    }
}
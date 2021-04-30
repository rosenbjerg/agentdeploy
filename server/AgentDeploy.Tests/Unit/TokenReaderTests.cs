using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class TokenReaderTests
    {
        [Test]
        public async Task TestTokenReader()
        {
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileReader>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("name: test-token\ndescription: a test token");
            var tokenReaderLoggerMock = new Mock<ILogger<TokenReader>>();
            
            var tokenReader = new TokenReader(directoryOptions, deserializer, fileReaderMock.Object, tokenReaderLoggerMock.Object);

            var token = await tokenReader.ParseTokenFile("test", CancellationToken.None);
            
            Assert.NotNull(token);
            Assert.AreEqual("test-token", token!.Name);
            Assert.AreEqual("a test token", token.Description);
        }
    }
    [Category("Unit")]
    public class ScriptReaderTests
    {
        [Test]
        public async Task TestScriptReader()
        {
            var operationContext = new OperationContext { OperationCancelled = CancellationToken.None };
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileReader>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("command: echo test\nconcurrency: pertoken");
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(operationContext, deserializer, fileReaderMock.Object, directoryOptions, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test");
            
            Assert.NotNull(script);
            Assert.AreEqual("test", script!.Name);
            Assert.AreEqual("echo test", script.Command);
            Assert.AreEqual(ConcurrentExecutionLevel.PerToken, script.Concurrency);
        }
    }
}
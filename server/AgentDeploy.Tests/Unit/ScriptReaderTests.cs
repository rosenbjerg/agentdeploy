using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using AgentDeploy.Yaml;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ScriptReaderTests
    {
        [Test]
        public async Task TestScriptReader()
        {
            var operationContext = new OperationContext { OperationCancelled = CancellationToken.None };
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new YamlStringEnumConverter())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("command: echo test");
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(operationContext, deserializer, fileReaderMock.Object, directoryOptions, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test");
            
            Assert.NotNull(script);
            Assert.AreEqual("test", script!.Name);
            Assert.AreEqual("echo test", script.Command);
        }
        
        [TestCase("full", ConcurrentExecutionLevel.Full)]
        [TestCase("per-token", ConcurrentExecutionLevel.PerToken)]
        [TestCase("pertoken", ConcurrentExecutionLevel.PerToken)]
        [TestCase("none", ConcurrentExecutionLevel.None)]
        public async Task TestScriptReader_ParseConcurrencyLevel(string yamlText, ConcurrentExecutionLevel concurrentExecutionLevel)
        {
            var operationContext = new OperationContext { OperationCancelled = CancellationToken.None };
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new YamlStringEnumConverter())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync($"command: echo test\nconcurrency: {yamlText}");
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(operationContext, deserializer, fileReaderMock.Object, directoryOptions, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test");
            
            Assert.NotNull(script);
            Assert.AreEqual("test", script!.Name);
            Assert.AreEqual("echo test", script.Command);
            Assert.AreEqual(concurrentExecutionLevel, script.Concurrency);
        }
        
        [Test]
        public async Task TestScriptReader_Null()
        {
            var operationContext = new OperationContext { OperationCancelled = CancellationToken.None };
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(default(string));
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(operationContext, deserializer, fileReaderMock.Object, directoryOptions, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test");
            
            Assert.Null(script);
        }
    }
}
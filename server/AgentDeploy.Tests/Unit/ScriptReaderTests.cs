using System.Threading;
using System.Threading.Tasks;
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
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new YamlStringEnumConverter())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("command: echo test");
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(directoryOptions, deserializer, fileReaderMock.Object, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test", It.IsAny<CancellationToken>());
            
            Assert.NotNull(script);
            Assert.AreEqual("test", script!.Name);
            Assert.AreEqual("echo test", script.Command);
        }
        
        [TestCase("integer", ScriptArgumentType.Integer)]
        [TestCase("int", ScriptArgumentType.Integer)]
        
        [TestCase("decimal", ScriptArgumentType.Decimal)]
        [TestCase("float", ScriptArgumentType.Decimal)]
        
        [TestCase("string", ScriptArgumentType.String)]
        
        [TestCase("boolean", ScriptArgumentType.Boolean)]
        [TestCase("bool", ScriptArgumentType.Boolean)]
        public async Task TestScriptReader_ParseScriptArgumentType(string yamlText, ScriptArgumentType scriptArgumentType)
        {
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new YamlStringEnumConverter())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync($"variables:\n  test:\n    type: {yamlText}\ncommand: echo test");
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(directoryOptions, deserializer, fileReaderMock.Object, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test", It.IsAny<CancellationToken>());
            
            Assert.NotNull(script);
            Assert.AreEqual("test", script!.Name);
            Assert.AreEqual("echo test", script.Command);
            Assert.AreEqual(1, script.Variables.Count);
            Assert.AreEqual(scriptArgumentType, script.Variables["test"]!.Type);
        }
        
        [Test]
        public async Task TestScriptReader_Null()
        {
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(default(string));
            var scriptReaderLoggerMock = new Mock<ILogger<ScriptReader>>();
            
            var scriptReader = new ScriptReader(directoryOptions, deserializer, fileReaderMock.Object, scriptReaderLoggerMock.Object);

            var script = await scriptReader.Load("test", It.IsAny<CancellationToken>());
            
            Assert.Null(script);
        }
    }
}
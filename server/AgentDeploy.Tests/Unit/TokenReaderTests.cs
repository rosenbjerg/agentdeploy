using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using YamlDotNet.Serialization;using YamlDotNet.Serialization.NamingConventions;

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
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns("test");
            fileReaderMock.Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync("name: test-token\ndescription: a test token");
            var tokenReaderLoggerMock = new Mock<ILogger<TokenReader>>();
            
            var tokenReader = new TokenReader(directoryOptions, deserializer, fileReaderMock.Object, tokenReaderLoggerMock.Object);

            var token = await tokenReader.ParseTokenFile("test", CancellationToken.None);
            
            Assert.NotNull(token);
            Assert.AreEqual("test-token", token!.Name);
            Assert.AreEqual("a test token", token.Description);
        }
        
        [Test]
        public async Task TestTokenReader_Null()
        {
            var directoryOptions = new DirectoryOptions { Scripts = "test", Tokens = "test" };
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var fileReaderMock = new Mock<IFileService>();
            fileReaderMock.Setup(s => s.FindFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>())).Returns(default(string));
            var tokenReaderLoggerMock = new Mock<ILogger<TokenReader>>();
            
            var tokenReader = new TokenReader(directoryOptions, deserializer, fileReaderMock.Object, tokenReaderLoggerMock.Object);

            var token = await tokenReader.ParseTokenFile("test", CancellationToken.None);
            
            Assert.Null(token);
        }
    }
}
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class PreprocessingTests
    {
        [TestCase("test.ext", "clamscan -i testDir/files/test.ext")]
        [TestCase("test file.ext", "clamscan -i 'testDir/files/test file.ext'")]
        public async Task FilePreprocessingTests(string fileName, string expectedCommand)
        {
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/',
                DefaultFilePreprocessing = "clamscan -i $(FilePath)",
                Shell = "bash"
            };
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            var fileService = new Mock<IFileService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var invocationContext = new ScriptInvocationContext
            {
                Files = new []
                {
                    new AcceptedScriptInvocationFile("test", fileName, null, () => Stream.Null)
                }
            };
            
            var service = new ScriptInvocationFileService(executionOptions, null!, processExecutionServiceMock.Object, fileService.Object, NullLogger<ScriptInvocationFileService>.Instance);
            await service.DownloadFiles(invocationContext, "testDir", CancellationToken.None);
            
            processExecutionServiceMock.Verify(s => s.Invoke("bash", $"-c \"{expectedCommand}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);

        }
        
        [Test]
        public void FilePreprocessingFails_ThrowsException()
        {
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/',
                DefaultFilePreprocessing = "clamscan -i $(FilePath)",
                Shell = "bash"
            };
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            var fileService = new Mock<IFileService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(1, Array.Empty<string>(), Array.Empty<string>()));
            var invocationContext = new ScriptInvocationContext
            {
                Files = new []
                {
                    new AcceptedScriptInvocationFile("test", "test.ext", null, () => Stream.Null)
                }
            };
            
            var service = new ScriptInvocationFileService(executionOptions, null!, processExecutionServiceMock.Object, fileService.Object, NullLogger<ScriptInvocationFileService>.Instance);
            Assert.ThrowsAsync<FilePreprocessingFailedException>(() => service.DownloadFiles(invocationContext, "testDir", CancellationToken.None));
        }
    }
}
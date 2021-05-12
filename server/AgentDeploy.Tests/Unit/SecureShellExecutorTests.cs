using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class SecureShellExecutorTests
    {
        [TestCase("user", "/path/to/my key", "/source dir")]
        [TestCase("admin", "key", "sourceDir")]
        public async Task ExplicitPrivateKeySecureShellExecutorTest(string username, string privateKeyPath, string sourceDir)
        {
            var pkPath = privateKeyPath.Contains(" ") ? $"\"{privateKeyPath}\"" : privateKeyPath;
            var targetDir = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}'" : $"/tmp/{sourceDir.TrimStart('/')}";
            var targetScript = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}/script.sh'" : $"/tmp/{sourceDir.TrimStart('/')}/script.sh";
            var scriptInvocationContext = new ScriptInvocationContext
            {
                SecureShellOptions = new SecureShellOptions
                {
                    Username = username,
                    PrivateKeyPath = privateKeyPath
                }
            };
            
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/'
            };
            var scriptTransformer = new ScriptTransformer(executionOptions, null!);
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new ExplicitPrivateKeySecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("scp", $"-rqi {pkPath} -o StrictHostKeyChecking=no -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-qtti {pkPath} -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"/bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-i {pkPath} -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>()), Times.Once);
        }
        
        [TestCase("user", "/source dir")]
        [TestCase("admin", "sourceDir")]
        public async Task ImplicitPrivateKeySecureShellExecutorTest(string username, string sourceDir)
        {
            var targetDir = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}'" : $"/tmp/{sourceDir.TrimStart('/')}";
            var targetScript = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}/script.sh'" : $"/tmp/{sourceDir.TrimStart('/')}/script.sh";
            var scriptInvocationContext = new ScriptInvocationContext
            {
                SecureShellOptions = new SecureShellOptions
                {
                    Username = username
                }
            };
            
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/'
            };
            var scriptTransformer = new ScriptTransformer(executionOptions, null!);
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new ImplicitPrivateKeySecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("scp", $"-rq -o StrictHostKeyChecking=no -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-qtt -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"/bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>()), Times.Once);
        }
        
        [TestCase("user", "/source dir")]
        [TestCase("admin", "sourceDir")]
        public async Task SshPassSecureShellExecutorTest(string username, string sourceDir)
        {
            var targetDir = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}'" : $"/tmp/{sourceDir.TrimStart('/')}";
            var targetScript = sourceDir.Contains(" ") ? $"'/tmp/{sourceDir.TrimStart('/')}/script.sh'" : $"/tmp/{sourceDir.TrimStart('/')}/script.sh";
            var passwordFile = sourceDir.Contains(" ") ? $"\"/tmp/{sourceDir.TrimStart('/')}/sshpass.txt\"" : $"/tmp/{sourceDir.TrimStart('/')}/sshpass.txt";
            var scriptInvocationContext = new ScriptInvocationContext
            {
                SecureShellOptions = new SecureShellOptions
                {
                    Username = username,
                    Password = "my-password"
                }
            };
            
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/',
                TempDir = "/tmp"
            };
            var fileService = new Mock<IFileService>();
            var scriptTransformer = new ScriptTransformer(executionOptions, fileService.Object);
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new SshPassSecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object, fileService.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} scp -rq -o StrictHostKeyChecking=no -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} ssh -qtt -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"/bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} ssh -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>()), Times.Once);
        }
        [TestCase("user", "/source dir")]
        [TestCase("admin", "sourceDir")]
        public async Task FilePreprocessingTests(string username, string sourceDir)
        {
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/',
                DefaultFilePreprocessing = "clamscan -i $(FilePath)"
            };
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            var fileService = new Mock<IFileService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            
            var service = new ScriptInvocationFileService(executionOptions, processExecutionServiceMock.Object, fileService.Object, NullLogger<ScriptInvocationFileService>.Instance);
            await service.DownloadFiles(new ScriptInvocationContext(), "testDir", CancellationToken.None);
            
            processExecutionServiceMock.Verify(s => s.Invoke("b", $"-f {passwordFile} ssh -o StrictHostKeyChecking=no -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>()), Times.Once);

        }
    }
}
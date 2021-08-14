using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Scripts;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class LocalExecutorTests
    {

        [TestCase("user", "/path/to/my key", "/source dir")]
        [TestCase("admin", "key", "sourceDir")]
        public async Task LocalScriptExecutor(string username, string privateKeyPath, string sourceDir)
        {
            var targetScript = sourceDir.Contains(" ") ? $"'{sourceDir}/script.sh'" : $"{sourceDir}/script.sh";
            var scriptInvocationContext = new ScriptInvocationContext();
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/'
            };
            var scriptTransformer = new ScriptTransformer(executionOptions, null!);
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new LocalScriptExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("/bin/sh", targetScript, It.IsAny<Action<string, bool>>(), sourceDir), Times.Once);
        }
    }
    
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
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new ExplicitPrivateKeySecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("scp", $"-rqi {pkPath} -o StrictHostKeyChecking=accept-new -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-qtti {pkPath} -o StrictHostKeyChecking=accept-new -p 22 {username}@host.docker.internal \"cd {targetDir} ; /bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-i {pkPath} -o StrictHostKeyChecking=accept-new -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
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
                    Username = username,
                    HostKeyChecking = HostKeyCheckingOptions.Off
                }
            };
            
            var executionOptions = new ExecutionOptions
            {
                DirectorySeparatorChar = '/'
            };
            var scriptTransformer = new ScriptTransformer(executionOptions, null!);
            var processExecutionServiceMock = new Mock<IProcessExecutionService>();
            processExecutionServiceMock
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new ImplicitPrivateKeySecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            processExecutionServiceMock.Verify(s => s.Invoke("scp", $"-rq -o StrictHostKeyChecking=off -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-qtt -o StrictHostKeyChecking=off -p 22 {username}@host.docker.internal \"cd {targetDir} ; /bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("ssh", $"-o StrictHostKeyChecking=off -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
        }
        
        [TestCase("user", "/tmp/source dir")]
        [TestCase("admin", "/tmp/sourceDir")]
        public async Task SshPassSecureShellExecutorTest(string username, string sourceDir)
        {
            var operationContext = new OperationContext();
            var targetDir = sourceDir.Contains(" ") ? $"'{sourceDir}'" : $"{sourceDir}";
            var targetScript = sourceDir.Contains(" ") ? $"'{sourceDir}/script.sh'" : $"{sourceDir}/script.sh";
            var passwordFile = $"/tmp/sshpass-{operationContext.CorrelationId}.txt";
            var scriptInvocationContext = new ScriptInvocationContext
            {
                SecureShellOptions = new SecureShellOptions
                {
                    Username = username,
                    Password = "my-password",
                    HostKeyChecking = HostKeyCheckingOptions.Yes
                },
                CorrelationId = operationContext.CorrelationId
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
                .Setup(s => s.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string, bool>>(), It.IsAny<string>()))
                .ReturnsAsync(new ProcessExecutionResult(0, Array.Empty<string>(), Array.Empty<string>()));
            var service = new SshPassSecureShellExecutor(executionOptions, scriptTransformer, processExecutionServiceMock.Object, fileService.Object, operationContext);
            var result = await service.Execute(scriptInvocationContext, sourceDir, _ => { });
            
            Assert.AreEqual(0, result);
            
            fileService.Verify(s => s.WriteTextAsync(passwordFile, scriptInvocationContext.SecureShellOptions.Password, It.IsAny<CancellationToken>()), Times.Exactly(3));
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} scp -rq -o StrictHostKeyChecking=yes -P 22 {sourceDir} {username}@host.docker.internal:{targetDir}", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} ssh -qtt -o StrictHostKeyChecking=yes -p 22 {username}@host.docker.internal \"cd {targetDir} ; /bin/sh {targetScript}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
            processExecutionServiceMock.Verify(s => s.Invoke("sshpass", $"-f {passwordFile} ssh -o StrictHostKeyChecking=yes -p 22 {username}@host.docker.internal \"rm -r {targetDir}\"", It.IsAny<Action<string, bool>>(), It.IsAny<string>()), Times.Once);
        }
    }
}
using System;
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
    public class ScriptExecutorFactoryTests
    {
        [TestCase(false, null, null, nameof(ILocalScriptExecutor))]
        
        [TestCase(true, "test", null, nameof(ISshPassSecureShellExecutor))]
        [TestCase(true, "test", "test", nameof(ISshPassSecureShellExecutor))]
        
        [TestCase(true, null, "test", nameof(IExplicitPrivateKeySecureShellExecutor))]
        [TestCase(true, null, null, nameof(IImplicitPrivateKeySecureShellExecutor))]
        public void ResolvesCorrect(bool setSshOptions, string? password, string? privateKeyPath, string scriptExecutorName)
        {
            var scriptInvocationContext = new ScriptInvocationContext();
            if (setSshOptions)
            {
                scriptInvocationContext.SecureShellOptions = new SecureShellOptions
                {
                    Password = password,
                    PrivateKeyPath = privateKeyPath
                };
            }
            
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(s => s.GetService(It.IsAny<Type>())).Returns<Type>(t =>
                {
                    var creator = typeof(Mock<>).MakeGenericType(t);
                    return ((Mock)Activator.CreateInstance(creator)!).Object;
                })
                .Callback<Type>(type => Assert.AreEqual(scriptExecutorName, type.Name));
            
            var factory = new ScriptExecutorFactory(serviceProviderMock.Object);

            factory.Build(scriptInvocationContext);
            
            serviceProviderMock.Verify(s => s.GetService(It.IsAny<Type>()), Times.Once);
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
            var scriptTransformer = new ScriptTransformer(new OperationContext(), executionOptions, null!);
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
            var scriptTransformer = new ScriptTransformer(new OperationContext(), executionOptions, null!);
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
            var scriptTransformer = new ScriptTransformer(new OperationContext(), executionOptions, fileService.Object);
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
    }
}
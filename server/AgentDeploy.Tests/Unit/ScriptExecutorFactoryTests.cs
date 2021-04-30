using System;
using AgentDeploy.Models;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.ScriptExecutors;
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
}
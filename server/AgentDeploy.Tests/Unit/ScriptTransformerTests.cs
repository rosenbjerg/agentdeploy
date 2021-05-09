using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ScriptTransformerTests
    {
        [Test]
        public async Task ScriptIsPreparedCorrectly()
        {
            var scriptInvocationContext = new ScriptInvocationContext
            {
                Arguments = new List<AcceptedScriptInvocationArgument> { new("var1", "Test", false) },
                EnvironmentVariables = new [] { new ScriptEnvironmentVariable("HELLO", "WORLD") },
                Script = new Script
                {
                    Command = "echo $(var1)"
                }
            };
            
            var operationContext = new OperationContext();
            var executionOptions = new ExecutionOptions();
            var fileService = new Mock<IFileService>();
            fileService.Setup(s => s.WriteText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback<string, string, CancellationToken>((path, content, _) =>
            {
                Assert.AreEqual($"test{Path.DirectorySeparatorChar}script.sh", path);
                Assert.AreEqual($"HELLO=WORLD{Environment.NewLine}echo Test", content);
            });
            var scriptTransformer = new ScriptTransformer(operationContext, executionOptions, fileService.Object);

            var command = await scriptTransformer.PrepareScriptFile(scriptInvocationContext, "test");
            
            Assert.AreEqual("echo Test", command);
            fileService.Verify(s => s.WriteText(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
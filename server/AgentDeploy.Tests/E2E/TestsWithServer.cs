using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace AgentDeploy.Tests.E2E
{
    public abstract class TestsWithServer
    {
        protected readonly SecureShellOptions? SecureShellOptions;

        public TestsWithServer(SecureShellOptions? secureShellOptions)
        {
            SecureShellOptions = secureShellOptions;
        }
        protected IHost Host = null!;

        [OneTimeSetUp]
        public async Task StartServer()
        {
            var server = Program.CreateHostBuilder<TestApiStartup>(Array.Empty<string>());
            Host = await server.StartAsync();
        }

        [OneTimeTearDown]
        public async Task StopServer()
        {
            await Host.StopAsync();
            Host.Dispose();
        }

        [TearDown]
        public void Reset()
        {
            Host.Services.GetRequiredService<Mock<ITokenReader>>().Reset();
            Host.Services.GetRequiredService<Mock<IScriptReader>>().Reset();
        }
        
        [Test]
        public async Task InvalidToken()
        {
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke script http://localhost:5000 -t test");
            Assert.NotZero(exitCode);
            Assert.AreEqual("The provided token is invalid", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task MissingScriptAccess()
        {
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?>(), Ssh = SecureShellOptions});
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("No script named 'test' is available", instance.ErrorData[0]);
        }

        [Test]
        public async Task ImplicitScriptAccess_ScriptNotFound()
        {
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("No script named 'test' is available", instance.ErrorData[0]);
        }
        
        [Test]
        public async Task ExplicitScriptAccess_ScriptNotFound()
        {
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("No script named 'test' is available", instance.ErrorData[0]);
        }

        [Test]
        public async Task ImplicitScriptAccess_ScriptExists()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.IsTrue(instance.OutputData[1].EndsWith("testing-123"));
        }
        
        [Test]
        public async Task ExplicitScriptAccess_ScriptExists()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.IsTrue(instance.OutputData[1].EndsWith("testing-123"));
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateVariables()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -v test_var=test test_var=test2");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
            Assert.AreEqual("test_var:", instance.ErrorData[1]);
            Assert.AreEqual("  Variable with same key already provided", instance.ErrorData[2]);
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateSecretVariables()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -s test_var=test test_var=test2");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
            Assert.AreEqual("test_var:", instance.ErrorData[1]);
            Assert.AreEqual("  Secret variable with same key already provided", instance.ErrorData[2]);
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateEnvironmentVariables()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -s test_var=test -e test=123 test=321");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
            Assert.AreEqual("test:", instance.ErrorData[1]);
            Assert.AreEqual("  Environment variable with same key already provided", instance.ErrorData[2]);
        }

        [Test]
        public async Task Websocket_Output()
        {
            It.IsAny<string>();
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --ws");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.IsTrue(instance.OutputData[1].EndsWith("testing-123"));
        }
        
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.Full, true)]
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.None, false)]
        public async Task ConcurrentExecution(string scriptName1, string scriptName2, string token1, string token2, ConcurrentExecutionLevel concurrencyLevel, bool success)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load(scriptName1, It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "sleep 1", Concurrency = concurrencyLevel, Name = scriptName1 });
            if (scriptName1 != scriptName2)
                scriptReaderMock.Setup(s => s.Load(scriptName2, It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "sleep 1", Concurrency = concurrencyLevel, Name = scriptName2 });
            
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile(token1, It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            if (token1 != token2) 
                tokenReaderMock.Setup(s => s.ParseTokenFile(token2, It.IsAny<CancellationToken>())).ReturnsAsync(new Token{ Ssh = SecureShellOptions });
            
            var task1 = E2ETestUtils.ClientOutput($"invoke {scriptName1} http://localhost:5000 -t {token1}");
            await Task.Delay(100);
            var task2 = E2ETestUtils.ClientOutput($"invoke {scriptName2} http://localhost:5000 -t {token2}");

            var result = await Task.WhenAll(task1, task2);
            var task1Result = result[0];
            var task2Result = result[1];
            
            if (success)
            {
                AssertNoBackendExceptions();
                Assert.Zero(task1Result.exitCode);
                Assert.Zero(task2Result.exitCode);
            }
            else
            {
                Assert.Zero(task1Result.exitCode);
                Assert.NotZero(task2Result.exitCode);
                Assert.AreEqual($"The script '{scriptName2}' is currently locked. Try again later", task2Result.instance.ErrorData[0]);
            }
        }
        
        [TestCase("127.0.0.1", true)]
        [TestCase("127.0.0.0-127.0.0.10", true)]
        [TestCase("127.0.0.2-127.0.0.10", false)]
        [TestCase("128.0.0.1", false)]
        [TestCase("128.0.0.1-128.0.0.10", false)]
        public async Task TrustedIpFilter(string trustedIp, bool success)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { TrustedIps = new List<string>{ trustedIp }, AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");

            if (success)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
                Assert.IsTrue(instance.OutputData[1].EndsWith("testing-123"));
            }
            else
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual("The provided token is invalid", instance.ErrorData[0]);
            }
        }
        
        [Test]
        public async Task HiddenHeaders()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.IsTrue(instance.OutputData[0].EndsWith("testing-123"));
            Assert.AreEqual(1, instance.OutputData.Count);
        }
        
        [Test]
        public async Task HiddenTimestamps()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual("testing-123", instance.OutputData[1]);
        }
        
        [Test]
        public async Task HiddenHeadersAndTimestamps()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123"});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual("testing-123", instance.OutputData[0]);
            Assert.AreEqual(1, instance.OutputData.Count);
        }
        
        [Test]
        public async Task LineNumberFormat()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123\n\necho again", ShowCommand = true, ShowOutput = false });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual(3, instance.OutputData.Count);
            Assert.AreEqual("1 | echo testing-123", instance.OutputData[0]);
            Assert.AreEqual("2 | ", instance.OutputData[1]);
            Assert.AreEqual("3 | echo again", instance.OutputData[2]);
        }
        
        [Test]
        public async Task PreprocessingFailed()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo testing-123\n\necho again", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test", new ScriptFileDefinition
                {
                    FilePreprocessing = "exit 1"
                }}
            }});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });

            var tempFile = Path.Combine(Path.GetTempPath(), "test_file.ext");
            await File.WriteAllTextAsync(tempFile, "test");
            try
            {
                var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test -f test={tempFile}");
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("File preprocessing failed with non-zero exit-code: 1", instance.ErrorData[0]);
                Assert.AreEqual("test:", instance.ErrorData[1]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
        
        [TestCase(10, 100, null, true)]
        [TestCase(10, 100, "txt", true)]
        [TestCase(1000, 10000, "txt", false)]
        [TestCase(0, 10, "txt", false)]
        [TestCase(1000, 10000, null, false)]
        [TestCase(0, 10, null, false)]
        [TestCase(10, 100, "json", false)]
        public async Task FileInput(long minSize, long maxSize, string acceptedExtension, bool success)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "cat $(test_file)", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test_file", new ScriptFileDefinition
                {
                    MaxSize = maxSize,
                    MinSize = minSize,
                    AcceptedExtensions = string.IsNullOrEmpty(acceptedExtension) ? null : new [] { acceptedExtension }
                }}
            } });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -f test_file=E2E/Files/testfile.txt");

            if (success)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
                Assert.AreEqual(1, instance.OutputData.Count);
                Assert.AreEqual("the quick brown fox jumps over the lazy dog", instance.OutputData[0]);
            }
            else
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("test_file:", instance.ErrorData[1]);
            }
        }
        
        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public async Task RequiredFile(bool optional, bool provideFile, bool success)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "echo test", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test_file", new ScriptFileDefinition
                {
                    Optional = optional
                }}
            } });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps{(provideFile ? " -f test_file=E2E/Files/testfile.txt" : "")}");

            if (success)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
                Assert.AreEqual(1, instance.OutputData.Count);
                Assert.AreEqual("test", instance.OutputData[0]);
            }
            else
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("test_file:", instance.ErrorData[1]);
                Assert.AreEqual("  No file provided", instance.ErrorData[2]);
            }
        }

        private void AssertNoBackendExceptions()
        {
            var backendException = Host.Services.GetRequiredService<TestLoggerFactory>().GetExceptionStacktrace();
            Assert.IsEmpty(backendException);
        }
        
        [TestCase("testfile.txt", true)]
        [TestCase("*.txt", true)]
        [TestCase("notfound.jpeg", false)]
        [TestCase("*.jpeg", false)]
        public async Task AssetFile(string assetGlob, bool exists)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script { Command = "cat ./testfile.txt", Assets = new List<string> { assetGlob }});
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");

            
            if (exists)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
                Assert.AreEqual(1, instance.OutputData.Count);
                Assert.AreEqual("the quick brown fox jumps over the lazy dog", instance.OutputData[0]);
            }
            else
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("Missing files:", instance.ErrorData[1]);
                Assert.AreEqual($"  {assetGlob}", instance.ErrorData[2]);
            }
        }

        [Test]
        public async Task MissingArgument_NoDefaultValue()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual(3, instance.ErrorData.Count);
            Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
            Assert.AreEqual("test_var:", instance.ErrorData[1]);
            Assert.AreEqual("  No value provided", instance.ErrorData[2]);
        }

        [Test]
        public async Task MissingArgument_DefaultValue()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { DefaultValue = "testing-123" } }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual(1, instance.OutputData.Count);
            Assert.AreEqual("testing-123", instance.OutputData[0]);
        }

        [Test]
        public async Task LockedVariable_CannotProvide()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token
            {
                AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?>
                {
                    {
                        "test", new ScriptAccessDeclaration
                        {
                            LockedVariables = new Dictionary<string, string>
                            {
                                {"test_var", "testing_321"}
                            }
                        }
                    }
                }
            });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -v test_var=testing-123");
            
            Assert.NotZero(exitCode);
            Assert.AreEqual(3, instance.ErrorData.Count);
            Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
            Assert.AreEqual("test_var:", instance.ErrorData[1]);
            Assert.AreEqual("  Variable is locked and can not be provided", instance.ErrorData[2]);
        }
        
        [Test]
        public async Task LockedVariable_ValueIsValidated()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition
                    {
                        Regex = "testing"
                    } }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token
            {
                AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?>
                {
                    {
                        "test", new ScriptAccessDeclaration
                        {
                            LockedVariables = new Dictionary<string, string>
                            {
                                {"test_var", "testing_321"}
                            }
                        }
                    }
                }, Ssh = SecureShellOptions
            });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual(1, instance.OutputData.Count);
            Assert.AreEqual("testing_321", instance.OutputData[0]);
        }

        [Test]
        public async Task LockedVariable_ValueIsUsed()
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token
            {
                AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?>
                {
                    {
                        "test", new ScriptAccessDeclaration
                        {
                            LockedVariables = new Dictionary<string, string>
                            {
                                {"test_var", "testing_321"}
                            }
                        }
                    }
                }, Ssh = SecureShellOptions
            });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual(1, instance.OutputData.Count);
            Assert.AreEqual("testing_321", instance.OutputData[0]);
        }

        [TestCase("_mytestvar_", null, null, true)]
        [TestCase("_mytestvar_", "^_.*test.*_$", null, true)]
        [TestCase("_mytestvar", "^_.*test.*_$", null, false)]
        [TestCase("_mytestvar_", null, "^_.*test.*_$", true)]
        [TestCase("_mytestvar", null, "^_.*test.*_$", false)]
        [TestCase("_mytestvar_", "^_.*test.*_$", "^_.*test.*_$", true)]
        [TestCase("_mytestvar", "^_.*test.*_$", "^_.*test.*_$", false)]
        [TestCase("_mytestvar_", "^_.*test.*_$", "^_my", true)]
        [TestCase("_mytestvar", "^_.*test.*_$", "^my", false)]
        public async Task ContrainedVariables(string testValue, string scriptConstraint, string tokenConstraint, bool shouldSucceed)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Regex = scriptConstraint } }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token
            {
                AvailableScripts = !string.IsNullOrEmpty(tokenConstraint)
                    ? new Dictionary<string, ScriptAccessDeclaration?>
                    {
                        {
                            "test", new ScriptAccessDeclaration
                            {
                                VariableConstraints = new Dictionary<string, string>
                                {
                                    { "test_var", tokenConstraint }
                                }
                            }
                        }
                    }
                    : null, 
                Ssh = SecureShellOptions
            });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -v test_var={testValue}");
            
            if (shouldSucceed)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
                Assert.AreEqual(1, instance.OutputData.Count);
                Assert.AreEqual(testValue, instance.OutputData[0]);
            }
            else
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("One or more validation errors occured", instance.ErrorData[0]);
                Assert.AreEqual("test_var:", instance.ErrorData[1]);
                if (string.IsNullOrEmpty(scriptConstraint) && !string.IsNullOrEmpty(tokenConstraint))
                {
                    Assert.AreEqual($"  Provided value does not pass profile constraint regex validation ({tokenConstraint})", instance.ErrorData[2]);
                }
                else if (!string.IsNullOrEmpty(scriptConstraint))
                {
                    Assert.AreEqual($"  Provided value does not pass script regex validation ({scriptConstraint})", instance.ErrorData[2]);
                }
            }
        }

        
        [TestCase("1200", ScriptArgumentType.Integer, true)]
        [TestCase("12.1", ScriptArgumentType.Decimal, true)]
        [TestCase("test", ScriptArgumentType.String, true)]
        [TestCase("true", ScriptArgumentType.Boolean, true)]
        public async Task VariableValidation_InbuiltTypes(string variableValue, ScriptArgumentType scriptArgumentType, bool success)
        {
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Reset();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Type = scriptArgumentType } }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 --hide-timestamps -t test -v test_var={variableValue}");

            if (success)
            {
                AssertNoBackendExceptions();
                Assert.Zero(exitCode);
            }
            if (!success)
            {
                Assert.NotZero(exitCode);
                Assert.AreEqual(3, instance.ErrorData.Count);
                Assert.AreEqual("test_var:", instance.ErrorData[1]);
                Assert.IsTrue(instance.ErrorData[2].StartsWith("  Provided value does not pass type validation"));
            }
            else Assert.AreEqual(variableValue, instance.OutputData[1]);
        }
        
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task SecretsStaySecret(bool definedAsSecret, bool providedAsSecret)
        {
            const string secret = "myverysecretsecret";
            var scriptReaderMock = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            scriptReaderMock.Reset();
            scriptReaderMock.Setup(s => s.Load("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Script
            {
                Command = "echo $(test_var)",
                ShowOutput = true,
                ShowCommand = true,
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Secret = definedAsSecret } }
                }
            });
            var tokenReaderMock = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            tokenReaderMock.Setup(s => s.ParseTokenFile("test", It.IsAny<CancellationToken>())).ReturnsAsync(new Token { AvailableScripts = new Dictionary<string, ScriptAccessDeclaration?> { {"test", new ScriptAccessDeclaration()} }, Ssh = SecureShellOptions });
            
            var (exitCode, instance) = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 --hide-headers --hide-timestamps --hide-script-line-numbers -t test {(providedAsSecret ? "-s" : "-v")} test_var={secret}");

            var shouldBeSecret = definedAsSecret || providedAsSecret;
            AssertNoBackendExceptions();
            Assert.Zero(exitCode);
            Assert.AreEqual(2, instance.OutputData.Count);
            Assert.AreEqual(0, instance.ErrorData.Count);
            
            if (shouldBeSecret)
            {
                Assert.False(instance.OutputData.Any(s => s.Contains(secret)));
                Assert.False(instance.ErrorData.Any(s => s.Contains(secret)));
                Assert.AreEqual("echo " + new string('*', secret.Length), instance.OutputData[0]);
                Assert.AreEqual(new string('*', secret.Length), instance.OutputData[1]);
            }
            else
            {
                Assert.AreEqual("echo " + secret, instance.OutputData[0]);
                Assert.AreEqual(secret, instance.OutputData[1]);
            }
        }
    }
}
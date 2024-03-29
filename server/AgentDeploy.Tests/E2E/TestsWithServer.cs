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
            var result = await E2ETestUtils.ClientOutput("invoke script http://localhost:5000 -t test");
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("The provided token is invalid", result.ErrorData[0]);
        }

        private IEnumerable<T> DistinctBy<T, TKey>(IEnumerable<T> enumerable, Func<T, TKey> keySelector)
        {
            var uniqueKeys = new HashSet<TKey>();
            foreach (var element in enumerable)
            {
                if (uniqueKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        private void SetupMockedTokenReader(params (string name, Token token)[] tokens)
        {
            var mockedTokenReader = Host.Services.GetRequiredService<Mock<ITokenReader>>();
            foreach (var token in DistinctBy(tokens, t => t.name))
                mockedTokenReader.Setup(s => s.ParseTokenFile(token.name, It.IsAny<CancellationToken>())).ReturnsAsync(token.token);
        }
        private void SetupMockedScriptReader(params (string name, Script script)[] scripts)
        {
            var mockedScriptReader = Host.Services.GetRequiredService<Mock<IScriptReader>>();
            foreach (var script in DistinctBy(scripts, s => s.name))
                mockedScriptReader.Setup(s => s.Load(script.name, It.IsAny<CancellationToken>())).ReturnsAsync(script.script);
        }

        private static Dictionary<string, ScriptAccessDeclaration?> CreateScriptAccess(params (string, ScriptAccessDeclaration)[]? scriptAccess)
        {
            var result = new Dictionary<string, ScriptAccessDeclaration?>();
            if (scriptAccess != null)
            {
                foreach (var (name, scriptAccessDeclaration) in scriptAccess)
                {
                    result.Add(name, scriptAccessDeclaration);
                }
            }

            return result;
        }
        
        [Test]
        public async Task MissingScriptAccess()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(), Ssh = SecureShellOptions}));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("No script named 'test' is available", result.ErrorData[0]);
        }

        [Test]
        public async Task ImplicitScriptAccess_ScriptNotFound()
        {
            SetupMockedTokenReader(("test", new Token { Ssh = SecureShellOptions}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("No script named 'test' is available", result.ErrorData[0]);
        }
        
        [Test]
        public async Task ExplicitScriptAccess_ScriptNotFound()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));

            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("No script named 'test' is available", result.ErrorData[0]);
        }

        [Test]
        public async Task ImplicitScriptAccess_ScriptExists()
        {
            SetupMockedTokenReader(("test", new Token { Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.Zero(result.ExitCode);
            Assert.IsTrue(result.OutputData[1].EndsWith("testing-123"));
        }
        
        [Test]
        public async Task ExplicitScriptAccess_ScriptExists()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");
            
            Assert.Zero(result.ExitCode);
            Assert.IsTrue(result.OutputData[1].EndsWith("testing-123"));
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateVariables()
        {
            SetupMockedTokenReader(("test", new Token { Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));

            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -v test_var=test test_var=test2");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
            Assert.AreEqual("test_var:", result.ErrorData[1]);
            Assert.AreEqual("  Variable with same key already provided", result.ErrorData[2]);
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateSecretVariables()
        {
            SetupMockedTokenReader(("test", new Token{ Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -s test_var=test test_var=test2");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
            Assert.AreEqual("test_var:", result.ErrorData[1]);
            Assert.AreEqual("  Secret variable with same key already provided", result.ErrorData[2]);
        }
        
        [Test]
        public async Task ScriptInvocation_DuplicateEnvironmentVariables()
        {
            SetupMockedTokenReader(("test", new Token{ Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test -s test_var=test -e test=123 test=321");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
            Assert.AreEqual("test:", result.ErrorData[1]);
            Assert.AreEqual("  Environment variable with same key already provided", result.ErrorData[2]);
        }

        [Test]
        public async Task Websocket_Output()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --ws");
            
            Assert.Zero(result.ExitCode);
            Assert.IsTrue(result.OutputData[1].EndsWith("testing-123"));
        }
        
        [Test]
        public async Task Websocket_Command_Output()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123", ShowCommand = true }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --ws");
            
            Assert.Zero(result.ExitCode);
            Assert.IsTrue(result.OutputData.Contains("1 | echo testing-123"));
            Assert.IsTrue(result.OutputData[1].EndsWith("testing-123"));
        }
        
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.Full, true)]
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.None, false)]
        public async Task ConcurrentExecution(string scriptName1, string scriptName2, string token1, string token2, ConcurrentExecutionLevel concurrencyLevel, bool success)
        {
            SetupMockedTokenReader(
                (token1, new Token { AvailableScripts = CreateScriptAccess((scriptName1, new ScriptAccessDeclaration())), Ssh = SecureShellOptions }),
                (token2, new Token { AvailableScripts = CreateScriptAccess((scriptName2, new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            
            SetupMockedScriptReader(
                (scriptName1, new Script { Command = "sleep 1", Concurrency = concurrencyLevel, Name = scriptName1 }), 
                (scriptName2, new Script { Command = "sleep 1", Concurrency = concurrencyLevel, Name = scriptName2 }));
            
            
            var task1 = E2ETestUtils.ClientOutput($"invoke {scriptName1} http://localhost:5000 -t {token1}");
            await Task.Delay(100);
            var task2 = E2ETestUtils.ClientOutput($"invoke {scriptName2} http://localhost:5000 -t {token2}");

            var result = await Task.WhenAll(task1, task2);
            var task1Result = result[0];
            var task2Result = result[1];
            
            if (success)
            {
                Assert.Zero(task1Result.ExitCode);
                Assert.Zero(task2Result.ExitCode);
            }
            else
            {
                Assert.Zero(task1Result.ExitCode);
                Assert.NotZero(task2Result.ExitCode);
                Assert.AreEqual($"The script '{scriptName2}' is currently locked. Try again later", task2Result.ErrorData[0]);
            }
        }
        
        [Test, Timeout(8000)]
        public async Task CancelledExecution()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            
            SetupMockedScriptReader(("test", new Script { Command = "echo Hello; sleep 10; echo World" }));

            var started = DateTime.UtcNow;
            
            var tokenSource = new CancellationTokenSource();
            var task = E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --ws --hide-headers --hide-timestamps", tokenSource.Token);
            tokenSource.CancelAfter(TimeSpan.FromMilliseconds(1000));
            var result = await task;
            
            var elapsed = DateTime.UtcNow - started;
            Assert.True(elapsed.TotalSeconds < 2);
            Assert.AreEqual(1, result.OutputData.Count);
            Assert.AreEqual("Hello", result.OutputData[0]);
        }
        
        [TestCase("127.0.0.1", true)]
        [TestCase("127.0.0.0-127.0.0.10", true)]
        [TestCase("127.0.0.2-127.0.0.10", false)]
        [TestCase("128.0.0.1", false)]
        [TestCase("128.0.0.1-128.0.0.10", false)]
        public async Task TrustedIpFilter(string trustedIp, bool success)
        {
            SetupMockedTokenReader(("test", new Token { TrustedIps = new List<string>{ trustedIp }, AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test");

            if (success)
            {
                Assert.Zero(result.ExitCode);
                Assert.IsTrue(result.OutputData[1].EndsWith("testing-123"));
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual("The provided token is invalid", result.ErrorData[0]);
            }
        }
        
        [Test]
        public async Task HiddenHeaders()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers");
            
            Assert.Zero(result.ExitCode);
            Assert.IsTrue(result.OutputData[0].EndsWith("testing-123"));
            Assert.AreEqual(1, result.OutputData.Count);
        }
        
        [Test]
        public async Task HiddenTimestamps()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual("testing-123", result.OutputData[1]);
        }
        
        [Test]
        public async Task HiddenHeadersAndTimestamps()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123"}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual("testing-123", result.OutputData[0]);
            Assert.AreEqual(1, result.OutputData.Count);
        }
        
        [Test]
        public async Task LineNumberFormat()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123\n\necho again", ShowCommand = true, ShowOutput = false }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual(3, result.OutputData.Count);
            Assert.AreEqual("1 | echo testing-123", result.OutputData[0]);
            Assert.AreEqual("2 | ", result.OutputData[1]);
            Assert.AreEqual("3 | echo again", result.OutputData[2]);
        }
        
        [Test]
        public async Task PreprocessingFailed()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo testing-123\n\necho again", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test", new ScriptFileDefinition
                {
                    FilePreprocessing = "exit 1"
                }}
            }}));

            var tempFile = Path.Combine(Path.GetTempPath(), "test_file.ext");
            await File.WriteAllTextAsync(tempFile, "test");
            try
            {
                var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test -f test={tempFile}");
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("File preprocessing failed with non-zero exit-code: 1", result.ErrorData[0]);
                Assert.AreEqual("test:", result.ErrorData[1]);
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
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "cat $(test_file)", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test_file", new ScriptFileDefinition
                {
                    MaxSize = maxSize,
                    MinSize = minSize,
                    AcceptedExtensions = string.IsNullOrEmpty(acceptedExtension) ? null : new [] { acceptedExtension }
                }}
            }}));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -f test_file=E2E/Files/testfile.txt");

            if (success)
            {
                Assert.Zero(result.ExitCode);
                Assert.AreEqual(1, result.OutputData.Count);
                Assert.AreEqual("the quick brown fox jumps over the lazy dog", result.OutputData[0]);
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("test_file:", result.ErrorData[1]);
            }
        }
        
        [TestCase(false, false, false)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, true)]
        public async Task RequiredFile(bool optional, bool provideFile, bool success)
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "echo test", Files = new Dictionary<string, ScriptFileDefinition?>
            {
                {"test_file", new ScriptFileDefinition
                {
                    Optional = optional
                }}
            }}));
            
            var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps{(provideFile ? " -f test_file=E2E/Files/testfile.txt" : "")}");

            if (success)
            {
                Assert.Zero(result.ExitCode);
                Assert.AreEqual(1, result.OutputData.Count);
                Assert.AreEqual("test", result.OutputData[0]);
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("test_file:", result.ErrorData[1]);
                Assert.AreEqual("  No file provided", result.ErrorData[2]);
            }
        }
        
        [TestCase("testfile.txt", true)]
        [TestCase("*.txt", true)]
        [TestCase("notfound.jpeg", false)]
        [TestCase("*.jpeg", false)]
        public async Task AssetFile(string assetGlob, bool exists)
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script { Command = "cat ./testfile.txt", Assets = new List<string> { assetGlob }}));
            
            var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            if (exists)
            {
                Assert.Zero(result.ExitCode);
                Assert.AreEqual(1, result.OutputData.Count);
                Assert.AreEqual("the quick brown fox jumps over the lazy dog", result.OutputData[0]);
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("Missing files:", result.ErrorData[1]);
                Assert.AreEqual($"  {assetGlob}", result.ErrorData[2]);
            }
        }

        [Test]
        public async Task MissingArgument_NoDefaultValue()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual(3, result.ErrorData.Count);
            Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
            Assert.AreEqual("test_var:", result.ErrorData[1]);
            Assert.AreEqual("  No value provided", result.ErrorData[2]);
        }

        [Test]
        public async Task MissingArgument_DefaultValue()
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { DefaultValue = "testing-123" } }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual(1, result.OutputData.Count);
            Assert.AreEqual("testing-123", result.OutputData[0]);
        }

        [Test]
        public async Task LockedVariable_CannotProvide()
        {
            SetupMockedTokenReader(("test", new Token
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
            }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -v test_var=testing-123");
            
            Assert.NotZero(result.ExitCode);
            Assert.AreEqual(3, result.ErrorData.Count);
            Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
            Assert.AreEqual("test_var:", result.ErrorData[1]);
            Assert.AreEqual("  Variable is locked and can not be provided", result.ErrorData[2]);
        }
        
        [Test]
        public async Task LockedVariable_ValueIsValidated()
        {
            SetupMockedTokenReader(("test", new Token
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
            }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition
                    {
                        Regex = "testing"
                    } }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual(1, result.OutputData.Count);
            Assert.AreEqual("testing_321", result.OutputData[0]);
        }

        [Test]
        public async Task LockedVariable_ValueIsUsed()
        {
            SetupMockedTokenReader(("test", new Token
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
            }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition() }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput("invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps");
            
            Assert.Zero(result.ExitCode);
            Assert.AreEqual(1, result.OutputData.Count);
            Assert.AreEqual("testing_321", result.OutputData[0]);
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
        public async Task ConstrainedVariables(string testValue, string scriptConstraint, string tokenConstraint, bool shouldSucceed)
        {
            SetupMockedTokenReader(("test", new Token
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
            }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Regex = scriptConstraint } }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 -t test --hide-headers --hide-timestamps -v test_var={testValue}");
            
            if (shouldSucceed)
            {
                Assert.Zero(result.ExitCode);
                Assert.AreEqual(1, result.OutputData.Count);
                Assert.AreEqual(testValue, result.OutputData[0]);
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("One or more validation errors occured", result.ErrorData[0]);
                Assert.AreEqual("test_var:", result.ErrorData[1]);
                if (string.IsNullOrEmpty(scriptConstraint) && !string.IsNullOrEmpty(tokenConstraint))
                {
                    Assert.AreEqual($"  Provided value does not pass profile constraint regex validation ({tokenConstraint})", result.ErrorData[2]);
                }
                else if (!string.IsNullOrEmpty(scriptConstraint))
                {
                    Assert.AreEqual($"  Provided value does not pass script regex validation ({scriptConstraint})", result.ErrorData[2]);
                }
            }
        }

        
        [TestCase("1200", ScriptArgumentType.Integer, true)]
        [TestCase("12.1", ScriptArgumentType.Decimal, true)]
        [TestCase("test", ScriptArgumentType.String, true)]
        [TestCase("true", ScriptArgumentType.Boolean, true)]
        public async Task VariableValidation_InbuiltTypes(string variableValue, ScriptArgumentType scriptArgumentType, bool success)
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Type = scriptArgumentType } }
                }
            }));
            
            var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 --hide-timestamps -t test -v test_var={variableValue}");

            if (success)
            {
                Assert.Zero(result.ExitCode);
                Assert.AreEqual(variableValue, result.OutputData[1]);
            }
            else
            {
                Assert.NotZero(result.ExitCode);
                Assert.AreEqual(3, result.ErrorData.Count);
                Assert.AreEqual("test_var:", result.ErrorData[1]);
                Assert.IsTrue(result.ErrorData[2].StartsWith("  Provided value does not pass type validation"));
            }
        }
        
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public async Task SecretsStaySecret(bool definedAsSecret, bool providedAsSecret)
        {
            SetupMockedTokenReader(("test", new Token { AvailableScripts = CreateScriptAccess(("test", new ScriptAccessDeclaration())), Ssh = SecureShellOptions }));
            SetupMockedScriptReader(("test", new Script
            {
                Command = "echo $(test_var)",
                ShowOutput = true,
                ShowCommand = true,
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    { "test_var", new ScriptVariableDefinition { Secret = definedAsSecret } }
                }
            }));
            
            const string secret = "myverysecretsecret";
            
            var result = await E2ETestUtils.ClientOutput($"invoke test http://localhost:5000 --hide-headers --hide-timestamps --hide-script-line-numbers -t test {(providedAsSecret ? "-s" : "-v")} test_var={secret}");

            var shouldBeSecret = definedAsSecret || providedAsSecret;
            Assert.Zero(result.ExitCode);
            Assert.AreEqual(2, result.OutputData.Count);
            Assert.AreEqual(0, result.ErrorData.Count);
            
            if (shouldBeSecret)
            {
                Assert.False(result.OutputData.Any(s => s.Contains(secret)));
                Assert.False(result.ErrorData.Any(s => s.Contains(secret)));
                Assert.AreEqual("echo " + new string('*', secret.Length), result.OutputData[0]);
                Assert.AreEqual(new string('*', secret.Length), result.OutputData[1]);
            }
            else
            {
                Assert.AreEqual("echo " + secret, result.OutputData[0]);
                Assert.AreEqual(secret, result.OutputData[1]);
            }
        }
    }
}
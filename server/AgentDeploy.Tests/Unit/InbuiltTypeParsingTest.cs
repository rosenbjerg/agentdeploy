using System;
using System.Collections.Generic;
using System.IO;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.TypeValidation;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class TypeValidationServiceTests
    {
        private TypeValidationService _service = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            _service = new TypeValidationService();
        }

        [TestCase("12.1", ScriptArgumentType.Integer, null, null, null, null, false)]
        [TestCase("1200", ScriptArgumentType.Integer, null, null, null, null, true)]
        [TestCase("1200", ScriptArgumentType.Integer, "^abc$", null, null, null, false)]
        [TestCase("1200", ScriptArgumentType.Integer, null, null, null, "^abc$", false)]
        [TestCase("1200", ScriptArgumentType.Integer, null, null, null, "[120]+", true)]
        [TestCase("1200", ScriptArgumentType.Integer, "[120]+", null, null, "[120]+", true)]
        [TestCase("1200", ScriptArgumentType.Integer, "[120]+", "1200", null, "[120]+", true)]
        [TestCase(null, ScriptArgumentType.Integer, "[120]+", "1200", null, "[120]+", true)]
        [TestCase(null, ScriptArgumentType.Integer, "[120]+", "1200", "1200", "[120]+", true)]
        [TestCase("1200", ScriptArgumentType.Integer, "[120]+", "1200", "1200", "[120]+", false)]
        [TestCase("1200", ScriptArgumentType.Integer, "^abc$", null, "1200", null, false)]
        [TestCase(null, ScriptArgumentType.Integer, "[120]+", null, "1200", null, true)]
        [TestCase(null, ScriptArgumentType.Integer, null, "test", null, null, false)]
        [TestCase(null, ScriptArgumentType.Integer, null, "1200", null, null, true)]
        [TestCase("test1200", ScriptArgumentType.Integer, null, null, null, null, false)]
        [TestCase("1200test", ScriptArgumentType.Integer, null, null, null, null, false)]
        [TestCase("test", ScriptArgumentType.Integer, null, null, null, null, false)]
        [TestCase("12.1", ScriptArgumentType.Decimal, null, null, null, null, true)]
        [TestCase("test12.1", ScriptArgumentType.Decimal, null, null, null, null, false)]
        [TestCase("12.1test", ScriptArgumentType.Decimal, null, null, null, null, false)]
        [TestCase("1200", ScriptArgumentType.Decimal, null, null, null, null, false)]
        [TestCase("test", ScriptArgumentType.Decimal, null, null, null, null, false)]
        [TestCase("12.1", ScriptArgumentType.String, null, null, null, null, true)]
        [TestCase("1200", ScriptArgumentType.String, null, null, null, null, true)]
        [TestCase("test", ScriptArgumentType.String, null, null, null, null, true)]
        [TestCase("true", ScriptArgumentType.Boolean, null, null, null, null, true)]
        [TestCase("True", ScriptArgumentType.Boolean, null, null, null, null, false)]
        [TestCase("TRUE", ScriptArgumentType.Boolean, null, null, null, null, false)]
        [TestCase("false", ScriptArgumentType.Boolean, null, null, null, null, true)]
        [TestCase("False", ScriptArgumentType.Boolean, null, null, null, null, false)]
        [TestCase("FALSE", ScriptArgumentType.Boolean, null, null, null, null, false)]
        [TestCase("mail@mail.com", ScriptArgumentType.Email, null, null, null, null, true)]
        [TestCase("mailmail.com", ScriptArgumentType.Email, null, null, null, null, false)]
        [TestCase("@mail.com", ScriptArgumentType.Email, null, null, null, null, false)]
        [TestCase("test", ScriptArgumentType.Email, null, null, null, null, false)]
        [TestCase("::1", ScriptArgumentType.Hostname, null, null, null, null, true)]
        [TestCase("1.2.3.4", ScriptArgumentType.Hostname, null, null, null, null, true)]
        [TestCase("mail.com", ScriptArgumentType.Hostname, null, null, null, null, true)]
        [TestCase(".mail.com", ScriptArgumentType.Hostname, null, null, null, null, false)]
        [TestCase("mail.com", ScriptArgumentType.DnsName, null, null, null, null, true)]
        [TestCase(".mail.com", ScriptArgumentType.DnsName, null, null, null, null, false)]
        [TestCase("1.2.3.4", ScriptArgumentType.DnsName, null, null, null, null, false)]
        [TestCase("::1", ScriptArgumentType.DnsName, null, null, null, null, false)]
        [TestCase("1.2.3.4", ScriptArgumentType.IP, null, null, null, null, true)]
        [TestCase("mail.com", ScriptArgumentType.IP, null, null, null, null, false)]
        [TestCase("test", ScriptArgumentType.IP, null, null, null, null, false)]
        [TestCase("1.2.3.4", ScriptArgumentType.IPv4, null, null, null, null, true)]
        [TestCase("::1", ScriptArgumentType.IPv4, null, null, null, null, false)]
        [TestCase("1.2.3.256", ScriptArgumentType.IPv4, null, null, null, null, false)]
        [TestCase("test", ScriptArgumentType.IPv4, null, null, null, null, false)]
        [TestCase("1.2.3.4", ScriptArgumentType.IPv6, null, null, null, null, false)]
        [TestCase("::1", ScriptArgumentType.IPv6, null, null, null, null, true)]
        [TestCase("test", ScriptArgumentType.IPv6, null, null, null, null, false)]
        public void TestValidationOfVariableInput(string? yamlValue, ScriptArgumentType scriptArgumentType,
            string? regex, string? defaultValue, string? lockedValue, string? constraint, bool success)
        {
            var parsedScriptInvocation = new ParsedScriptInvocation();
            if (yamlValue != null)
                parsedScriptInvocation.Variables["test"] = new ScriptInvocationVariable("test", yamlValue, false);

            var script = new Script
            {
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    {
                        "test", new ScriptVariableDefinition
                        {
                            Type = scriptArgumentType,
                            Regex = regex,
                            DefaultValue = defaultValue
                        }
                    }
                },
            };

            var scriptAccessDeclaration = new ScriptAccessDeclaration();
            if (lockedValue != null) scriptAccessDeclaration.LockedVariables["test"] = lockedValue;
            if (constraint != null) scriptAccessDeclaration.VariableConstraints["test"] = constraint;

            if (!success)
            {
                var exception = Assert.Throws<FailedInvocationValidationException>(() =>
                    _service.Validate(parsedScriptInvocation, script, scriptAccessDeclaration));
            }
            else
            {
                var result = _service.Validate(parsedScriptInvocation, script, scriptAccessDeclaration);
                Assert.AreEqual(1, result.AcceptedVariables.Count);
            }
        }
        
        [TestCase("test", ".txt", 100, false, 200, 0, null, true)]
        [TestCase("test", ".txt", 100, false, 200, 0, "txt", true)]
        [TestCase("test", ".mp3", 100, false, 200, 0, "txt", false)]
        [TestCase("test", ".txt", 100, false, 50, 0, null, false)]
        [TestCase("test", ".txt", 100, false, 200, 150, null, false)]
        [TestCase(null, ".txt", 100, true, 200, 0, null, true)]
        [TestCase(null, ".txt", 100, false, 200, 0, null, false)]
        public void TestValidationOfFileInput(string? key, string fileExtension, long fileSize, bool optional, long maxSize, long minSize, string? acceptedExtension, bool success)
        {
            var parsedScriptInvocation = new ParsedScriptInvocation();
            if (key != null)
            {
                parsedScriptInvocation.Files.Add(key, new ScriptInvocationFile
                {
                    Key = key,
                    Read = () => Stream.Null,
                    FileName = $"test{fileExtension}",
                    FileSize = fileSize
                });
            }

            var script = new Script
            {
                Files = new Dictionary<string, ScriptFileDefinition?>
                {
                    {
                        "test", new ScriptFileDefinition
                        {
                            Optional = optional,
                            MaxSize = maxSize,
                            MinSize = minSize
                        }
                    }
                }
            };
            if (acceptedExtension != null) script.Files["test"]!.AcceptedExtensions = new[] { acceptedExtension };

            var scriptAccessDeclaration = new ScriptAccessDeclaration();
            if (!success)
            {
                var exception = Assert.Throws<FailedInvocationValidationException>(() => _service.Validate(parsedScriptInvocation, script, scriptAccessDeclaration));
            }
            else
            {
                var result = _service.Validate(parsedScriptInvocation, script, scriptAccessDeclaration);
                Assert.AreEqual(1, result.AcceptedFiles.Count);
            }
        }
    }
}
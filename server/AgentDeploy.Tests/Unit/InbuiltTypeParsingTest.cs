using System.Collections.Generic;
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

        [TestCase("12.1", ScriptArgumentType.Integer, false)]
        [TestCase("1200", ScriptArgumentType.Integer, true)]
        [TestCase("test1200", ScriptArgumentType.Integer, false)]
        [TestCase("1200test", ScriptArgumentType.Integer, false)]
        [TestCase("test", ScriptArgumentType.Integer, false)]
        
        [TestCase("12.1", ScriptArgumentType.Decimal, true)]
        [TestCase("test12.1", ScriptArgumentType.Decimal, false)]
        [TestCase("12.1test", ScriptArgumentType.Decimal, false)]
        [TestCase("1200", ScriptArgumentType.Decimal, false)]
        [TestCase("test", ScriptArgumentType.Decimal, false)]
        
        [TestCase("12.1", ScriptArgumentType.String, true)]
        [TestCase("1200", ScriptArgumentType.String, true)]
        [TestCase("test", ScriptArgumentType.String, true)]
        
        [TestCase("true", ScriptArgumentType.Boolean, true)]
        [TestCase("True", ScriptArgumentType.Boolean, false)]
        [TestCase("TRUE", ScriptArgumentType.Boolean, false)]
        [TestCase("false", ScriptArgumentType.Boolean, true)]
        [TestCase("False", ScriptArgumentType.Boolean, false)]
        [TestCase("FALSE", ScriptArgumentType.Boolean, false)]
        
        [TestCase("mail@mail.com", ScriptArgumentType.Email, true)]
        [TestCase("mailmail.com", ScriptArgumentType.Email, false)]
        [TestCase("@mail.com", ScriptArgumentType.Email, false)]
        [TestCase("test", ScriptArgumentType.Email, false)]
        
        [TestCase("::1", ScriptArgumentType.Hostname, true)]
        [TestCase("1.2.3.4", ScriptArgumentType.Hostname, true)]
        [TestCase("mail.com", ScriptArgumentType.Hostname, true)]
        [TestCase(".mail.com", ScriptArgumentType.Hostname, false)]
        
        [TestCase("mail.com", ScriptArgumentType.DnsName, true)]
        [TestCase(".mail.com", ScriptArgumentType.DnsName, false)]
        [TestCase("1.2.3.4", ScriptArgumentType.DnsName, false)]
        [TestCase("::1", ScriptArgumentType.DnsName, false)]
        
        [TestCase("1.2.3.4", ScriptArgumentType.IP, true)]
        [TestCase("mail.com", ScriptArgumentType.IP, false)]
        [TestCase("test", ScriptArgumentType.IP, false)]
        
        [TestCase("1.2.3.4", ScriptArgumentType.IPv4, true)]
        [TestCase("::1", ScriptArgumentType.IPv4, false)]
        [TestCase("1.2.3.256", ScriptArgumentType.IPv4, false)]
        [TestCase("test", ScriptArgumentType.IPv4, false)]
        
        [TestCase("1.2.3.4", ScriptArgumentType.IPv6, false)]
        [TestCase("::1", ScriptArgumentType.IPv6, true)]
        [TestCase("test", ScriptArgumentType.IPv6, false)]

        public void TestValidationOfVariableInput(string yamlValue, ScriptArgumentType scriptArgumentType, bool success)
        {
            var parsedScriptInvocation = new ParsedScriptInvocation
            {
                Variables = new Dictionary<string, ScriptInvocationVariable>
                {
                    { "test", new ScriptInvocationVariable("test", yamlValue, false) }
                }
            };
            var script = new Script
            {
                Variables = new Dictionary<string, ScriptVariableDefinition?>
                {
                    {
                        "test", new ScriptVariableDefinition
                        {
                            Type = scriptArgumentType
                        }
                    }
                }
            };

            if (!success)
            {
                Assert.Throws<FailedInvocationValidationException>(() => _service.Validate(parsedScriptInvocation, script, new ScriptAccessDeclaration()));
            }
            else
            {
                var result = _service.Validate(parsedScriptInvocation, script, new ScriptAccessDeclaration());
                Assert.AreEqual(1, result.AcceptedVariables.Count);
            }
        }
    }
}
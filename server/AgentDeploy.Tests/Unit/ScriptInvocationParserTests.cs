using System;
using System.IO;
using System.Linq;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Services.Scripts;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ScriptInvocationParserTests
    {
        [TestCase(true, "test1", "test2")]
        [TestCase(false, "test1", "test1")]
        [TestCase(false, "test1", "test2", "test1")]
        public void ParseVariables(bool valid, params string[] keys)
        {
            var invocation = new ScriptInvocation
            {
                ScriptName = "test",
                WebsocketSessionId = Guid.NewGuid(),
                Variables = keys.Select(k => $"{k}=test").ToArray()
            };
            
            if (valid)
            {
                var result = ScriptInvocationParser.Parse(invocation);
                Assert.NotNull(result);
                Assert.AreEqual(invocation.ScriptName, result.ScriptName);
                Assert.AreEqual(invocation.WebsocketSessionId, result.WebsocketSessionId);
                Assert.AreEqual(0, result.EnvironmentVariables.Length);
                Assert.AreEqual(0, result.Files.Count);
                Assert.AreEqual(keys.Length, result.Variables.Count);
            }
            else
            {
                var exception = Assert.Throws<FailedInvocationValidationException>(() => ScriptInvocationParser.Parse(invocation));
                Assert.NotNull(exception);
                Assert.AreEqual(1, exception!.Errors.Count);
                Assert.AreEqual(keys.First(), exception.Errors.First().Key);
            }
        }

        [TestCase(true, "test1", "test2")]
        [TestCase(false, "test1", "test1")]
        [TestCase(false, "test1", "test2", "test1")]
        public void ParseEnvironmentVariables(bool valid, params string[] keys)
        {
            var invocation = new ScriptInvocation
            {
                ScriptName = "test",
                WebsocketSessionId = Guid.NewGuid(),
                EnvironmentVariables = keys.Select(k => $"{k}=test").ToArray()
            };
            
            if (valid)
            {
                var result = ScriptInvocationParser.Parse(invocation);
                Assert.NotNull(result);
                Assert.AreEqual(invocation.ScriptName, result.ScriptName);
                Assert.AreEqual(invocation.WebsocketSessionId, result.WebsocketSessionId);
                Assert.AreEqual(0, result.Variables.Count);
                Assert.AreEqual(0, result.Files.Count);
                Assert.AreEqual(keys.Length, result.EnvironmentVariables.Length);
            }
            else
            {
                var exception = Assert.Throws<FailedInvocationValidationException>(() => ScriptInvocationParser.Parse(invocation));
                Assert.NotNull(exception);
                Assert.AreEqual(1, exception!.Errors.Count);
                Assert.AreEqual(keys.First(), exception.Errors.First().Key);
            }
        }

        [TestCase(true, "test1", "test2")]
        [TestCase(false, "test1", "test1")]
        [TestCase(false, "test1", "test2", "test1")]
        public void ParseFormFiles(bool valid, params string[] fieldNames)
        {
            var invocation = new ScriptInvocation
            {
                ScriptName = "test",
                WebsocketSessionId = Guid.NewGuid()
            };
            invocation.Files = fieldNames
                .Select(fieldName => new FormFile(Stream.Null, 0, 0, fieldName, $"{fieldName}=test.txt"))
                .ToArray();
            
            if (valid)
            {
                var result = ScriptInvocationParser.Parse(invocation);
                Assert.NotNull(result);
                Assert.AreEqual(invocation.ScriptName, result.ScriptName);
                Assert.AreEqual(invocation.WebsocketSessionId, result.WebsocketSessionId);
                Assert.AreEqual(0, result.Variables.Count);
                Assert.AreEqual(0, result.EnvironmentVariables.Length);
                Assert.AreEqual(fieldNames.Length, result.Files.Count);
            }
            else
            {
                var exception = Assert.Throws<FailedInvocationValidationException>(() => ScriptInvocationParser.Parse(invocation));
                Assert.NotNull(exception);
                Assert.AreEqual(1, exception!.Errors.Count);
                Assert.AreEqual(fieldNames.First(), exception.Errors.First().Key);
            }
        }
    }
}
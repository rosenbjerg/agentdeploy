using System.Collections.Generic;
using AgentDeploy.Models;
using AgentDeploy.Services;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ReplacementUtilsTests
    {
        [TestCase("Hello World!", "Hello $(Subject)!", "Subject", "World")]
        [TestCase("Hello World!", "Hello $(subject)!", "subject", "World")]
        public void ReplaceVariable(string expectedResult, string script, string key, string value)
        {
            var actualResult = ReplacementUtils.ReplaceVariable(script, key, value);
            Assert.AreEqual(expectedResult, actualResult);
        }
        
        [TestCase("Hello World!", "Hello $(Subject)!", "Subject", "World")]
        [TestCase("Hello World!", "Hello $(subject)!", "subject", "World")]
        [TestCase("Hello $(Subject)!", "Hello $(Subject)!", "subject", "World")]
        public void ReplaceVariables(string expectedResult, string script, string key, string value)
        {
            var dictionary = new Dictionary<string, string>
            {
                {key, value}
            };
            
            var actualResult = ReplacementUtils.ReplaceVariables(script, dictionary);
            Assert.AreEqual(expectedResult, actualResult);
        }
        
        [TestCase("*** quick brown fox jumps over *** lazy dog", "the quick brown fox jumps over the lazy dog", "the")]
        [TestCase("the quick ***** fox jumps over the lazy dog", "the quick brown fox jumps over the lazy dog", "brown")]
        [TestCase("the quick brown *** jumps over the lazy dog", "the quick brown fox jumps over the lazy dog", "fox")]
        public void HideSecrets(string expectedResult, string script, string secret)
        {
            var scriptInvocationContext = new ScriptInvocationContext
            {
                Arguments = new List<AcceptedScriptInvocationArgument>
                {
                    new AcceptedScriptInvocationArgument("secret", secret, true)
                }
            };
            
            var actualResult = ReplacementUtils.HideSecrets(script, scriptInvocationContext);
            Assert.AreEqual(expectedResult, actualResult);
        }
        
        [TestCase("$(Hello) and Welcome $(World)", "Hello", "World")]
        [TestCase("$(Hello) and Welcome World", "Hello")]
        public void ExtractUsedVariables(string inputText, params string[] expectedVariables)
        {
            var actualResult = ReplacementUtils.ExtractUsedVariables(inputText);
            CollectionAssert.AreEquivalent(expectedVariables, actualResult);
        }
    }
}
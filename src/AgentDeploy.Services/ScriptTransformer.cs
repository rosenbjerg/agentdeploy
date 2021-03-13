using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;

namespace AgentDeploy.Services
{
    public class ScriptTransformer
    {
        public async Task<string> PrepareScriptFile(Script script, ScriptExecutionContext executionContext,
            string scriptFilePath, CancellationToken cancellationToken)
        {
            var argDict = executionContext.Arguments.ToDictionary(arg => arg.Name);
            var scriptText = ReplaceVariables(script, argDict);
            var finalText = string.Join(Environment.NewLine, executionContext.EnvironmentVariables) + Environment.NewLine + scriptText;
            await File.WriteAllTextAsync(scriptFilePath, finalText, cancellationToken);
            return scriptText;
        }

        public string HideSecrets(string text, IEnumerable<InvocationArgument> args)
        {
            var sb = new StringBuilder(text);
            foreach (var secret in args.Where(arg => arg.Secret))
            {
                sb.Replace(secret.Value, new string('*', secret.Value.Length));
            }

            return sb.ToString();
        }

        private static string ReplaceVariables(Script script, Dictionary<string, InvocationArgument> argDict)
        {
            return Regex.Replace(script.Command, @"\$\(([^)]+)\)", match =>
            {
                var key = match.Groups[1].Value;
                if (argDict.TryGetValue(key, out var value))
                    return value.Value;
                return match.Value;
            });
        }
    }
}
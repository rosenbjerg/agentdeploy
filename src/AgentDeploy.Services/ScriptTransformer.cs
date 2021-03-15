using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Services.Models;

namespace AgentDeploy.Services
{
    public class ScriptTransformer
    {
        private readonly IOperationContext _operationContext;

        public ScriptTransformer(IOperationContext operationContext)
        {
            _operationContext = operationContext;
        }
        
        public async Task<string> PrepareScriptFile(ScriptExecutionContext executionContext, string directory)
        {
            var scriptText = ReplaceVariables(executionContext.Script, executionContext);
            var finalText = string.Join(Environment.NewLine, executionContext.EnvironmentVariables) + Environment.NewLine + scriptText;
            var scriptFilePath = Path.Combine(directory, "script.sh");
            await File.WriteAllTextAsync(scriptFilePath, finalText, _operationContext.OperationCancelled);
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

        private string ReplaceVariables(Script script, ScriptExecutionContext executionContext)
        {
            var argDict = executionContext.Arguments.ToDictionary(arg => arg.Name);
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
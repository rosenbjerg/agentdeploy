using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;

namespace AgentDeploy.Services.Scripts
{
    public class ScriptTransformer : IScriptTransformer
    {
        private static readonly Regex VariableRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
        private readonly IOperationContext _operationContext;
        private readonly ExecutionOptions _executionOptions;

        public ScriptTransformer(IOperationContext operationContext, ExecutionOptions executionOptions)
        {
            _operationContext = operationContext;
            _executionOptions = executionOptions;
        }
        
        public async Task<string> PrepareScriptFile(ScriptInvocationContext invocationContext, string directory)
        {
            var scriptText = ReplaceVariables(invocationContext.Script.Command, invocationContext.Arguments.ToDictionary(arg => arg.Name, arg => arg.Value));
            var textVariables = new List<string>(invocationContext.EnvironmentVariables.Select(env => $"{env.Key}={env.Value}")) { scriptText.Trim() };
            var finalText =  string.Join(Environment.NewLine, textVariables);

            var scriptFilePath = BuildScriptPath(directory);
            await File.WriteAllTextAsync(scriptFilePath, finalText, _operationContext.OperationCancelled);
            
            return scriptText;
        }

        public string BuildScriptPath(string directory)
        {
            var scriptFilePath = Path.Combine(directory, $"script.{_executionOptions.ShellFileExtension.TrimStart('.')}");
            return scriptFilePath;
        }

        public string BuildScriptArgument(string scriptFilePath)
        {
            var variables = new Dictionary<string, string> { { "ScriptPath", $"\"{scriptFilePath}\"" } };
            var fileArgument = ReplaceVariables(_executionOptions.FileArgumentFormat, variables);
            return fileArgument;
        }

        public string HideSecrets(string text, ScriptInvocationContext invocationContext)
        {
            var sb = new StringBuilder(text);
            foreach (var secret in invocationContext.Arguments.Where(arg => arg.Secret && !string.IsNullOrWhiteSpace(arg.Value)))
            {
                sb.Replace(secret.Value, new string('*', secret.Value.Length));
            }

            return sb.ToString();
        }

        private string ReplaceVariables(string script, Dictionary<string, string> executionContext)
        {
            var argDict = executionContext;
            return VariableRegex.Replace(script, match =>
            {
                var key = match.Groups[1].Value;
                if (argDict.TryGetValue(key, out var value))
                    return value;
                return match.Value;
            });
        }
    }
}
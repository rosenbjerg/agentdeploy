using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services.Models;

namespace AgentDeploy.Services
{
    public class ScriptTransformer
    {
        private static Regex _variableRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
        private readonly IOperationContext _operationContext;
        private readonly ExecutionOptions _executionOptions;

        public ScriptTransformer(IOperationContext operationContext, ExecutionOptions executionOptions)
        {
            _operationContext = operationContext;
            _executionOptions = executionOptions;
        }
        
        public async Task<string> PrepareScriptFile(ScriptExecutionContext executionContext, string directory)
        {
            var scriptText = ReplaceVariables(executionContext.Script.Command, executionContext.Arguments.ToDictionary(arg => arg.Name, arg => arg.Value));
            var textVariables = new List<string>(executionContext.EnvironmentVariables) { scriptText.Trim() };
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

        public string HideSecrets(string text, ScriptExecutionContext executionContext)
        {
            var sb = new StringBuilder(text);
            foreach (var secret in executionContext.Arguments.Where(arg => arg.Secret))
            {
                sb.Replace(secret.Value, new string('*', secret.Value.Length));
            }

            return sb.ToString();
        }
        public ProcessOutput HideSecrets(ProcessOutput output, ScriptExecutionContext executionContext)
        {
            var sb = new StringBuilder(output.Output);
            foreach (var secret in executionContext.Arguments.Where(arg => arg.Secret))
            {
                sb.Replace(secret.Value, new string('*', secret.Value.Length));
            }

            return new ProcessOutput(output.Timestamp, sb.ToString(), output.Error);
        }

        public string ReplaceVariables(string script, Dictionary<string, string> executionContext)
        {
            var argDict = executionContext;
            return _variableRegex.Replace(script, match =>
            {
                var key = match.Groups[1].Value;
                if (argDict.TryGetValue(key, out var value))
                    return value;
                return match.Value;
            });
        }
    }
}
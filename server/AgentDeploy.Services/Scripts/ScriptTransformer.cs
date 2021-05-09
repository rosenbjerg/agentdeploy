using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptTransformer : IScriptTransformer
    {
        private static readonly Regex VariableRegex = new(@"\$\(([^)]+)\)", RegexOptions.Compiled);
        private readonly IOperationContext _operationContext;
        private readonly ExecutionOptions _executionOptions;
        private readonly IFileService _fileService;

        public ScriptTransformer(IOperationContext operationContext, ExecutionOptions executionOptions, IFileService fileService)
        {
            _operationContext = operationContext;
            _executionOptions = executionOptions;
            _fileService = fileService;
        }
        
        public async Task<string> PrepareScriptFile(ScriptInvocationContext invocationContext, string directory)
        {
            var scriptText = ReplaceVariables(invocationContext.Script.Command, invocationContext.Arguments.ToDictionary(arg => arg.Name, arg => arg.Value));
            var textVariables = new List<string>(invocationContext.EnvironmentVariables.Select(BuildEnvironmentVariable)) { scriptText.Trim() };
            var finalText =  string.Join(Environment.NewLine, textVariables);

            var scriptFilePath = BuildScriptPath(directory);
            await _fileService.WriteText(scriptFilePath, finalText, _operationContext.OperationCancelled);
            
            return scriptText;
        }

        public string BuildScriptPath(string directory)
        {
            var scriptFilePath = $"{directory}{_executionOptions.DirectorySeparatorChar}script.{_executionOptions.ShellFileExtension.TrimStart('.')}";
            return scriptFilePath;
        }

        public string BuildScriptArgument(string scriptFilePath)
        {
            var variables = new Dictionary<string, string> { { "ScriptPath", EscapeWhitespaceInPath(scriptFilePath, '\'') } };
            var fileArgument = ReplaceVariables(_executionOptions.FileArgumentFormat, variables);
            return fileArgument;
        }

        public string EscapeWhitespaceInPath(string path, char escapeChar)
        {
            if (path.Contains(" "))
                return $"{escapeChar}{path}{escapeChar}";
            return path;
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

        private string BuildEnvironmentVariable(ScriptEnvironmentVariable scriptEnvironmentVariable)
        {
            return ReplaceVariables(_executionOptions.EnvironmentVariableFormat, new Dictionary<string, string>
            {
                { "Key", scriptEnvironmentVariable.Key },
                { "Value", scriptEnvironmentVariable.Value }
            });
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
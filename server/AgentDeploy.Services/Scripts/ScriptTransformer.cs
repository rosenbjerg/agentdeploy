using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptTransformer : IScriptTransformer
    {
        private readonly ExecutionOptions _executionOptions;
        private readonly IFileService _fileService;

        public ScriptTransformer(ExecutionOptions executionOptions, IFileService fileService)
        {
            _executionOptions = executionOptions;
            _fileService = fileService;
        }
        
        public async Task<string[]> PrepareScriptFile(ScriptInvocationContext invocationContext, string directory,
            CancellationToken cancellationToken)
        {
            var scriptText = ReplacementUtils.ReplaceVariables(invocationContext.Script.Command, invocationContext.Arguments.ToDictionary(arg => arg.Name, arg => arg.Value));
            var textVariables = new List<string>(invocationContext.EnvironmentVariables.Select(BuildEnvironmentVariable)) { scriptText.Trim() };
            var finalText =  string.Join(_executionOptions.Linebreak, textVariables);

            var scriptFilePath = BuildScriptPath(directory);
            await _fileService.WriteText(scriptFilePath, finalText, cancellationToken);
            
            return scriptText.Split(new []{'\r', '\n'}, StringSplitOptions.None);
        }

        public string BuildScriptPath(string directory)
        {
            var scriptFileName = $"script.{_executionOptions.ShellFileExtension.TrimStart('.')}";
            var scriptFilePath = PathUtils.Combine(_executionOptions.DirectorySeparatorChar, directory, scriptFileName);
            return scriptFilePath;
        }

        public string BuildScriptArgument(string scriptFilePath)
        {
            var fileArgument = ReplacementUtils.ReplaceVariable(_executionOptions.FileArgumentFormat, "ScriptPath", PathUtils.EscapeWhitespaceInPath(scriptFilePath, '\''));
            return fileArgument;
        }


        private string BuildEnvironmentVariable(ScriptEnvironmentVariable scriptEnvironmentVariable)
        {
            return ReplacementUtils.ReplaceVariables(_executionOptions.EnvironmentVariableFormat, new Dictionary<string, string>
            {
                { "Key", scriptEnvironmentVariable.Key },
                { "Value", scriptEnvironmentVariable.Value }
            });
        }
    }
}
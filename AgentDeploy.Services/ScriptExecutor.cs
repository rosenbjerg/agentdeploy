using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.ExternalApi.Options;
using AgentDeploy.Services.Models;
using Instances;

namespace AgentDeploy.Services
{
    public record ExecutionResult(string Output, string Command, int ExitCode);
    public class ScriptExecutor
    {
        private readonly ExecutionOptions _executionOptions;

        public ScriptExecutor(ExecutionOptions executionOptions)
        {
            _executionOptions = executionOptions;
        }
        public async Task<ExecutionResult> Execute(Script script, ReadOnlyCollection<InvocationArgument> args, string[] environmentVariables)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.sh");
            try
            {
                var scriptText = await PrepareScriptFile(script, args, environmentVariables, tempFilePath);
                var startInfo = PrepareStartInfo(environmentVariables, tempFilePath);
                var instance = new Instance(startInfo);
                
                var exitCode = await instance.FinishedRunning();
                var visibleOutput = script.ShowOutput ? HideSecrets(string.Join('\n', instance.OutputData), args) : string.Empty;
                var visibleCommand = script.ShowCommand ? HideSecrets(scriptText, args) : string.Empty;
                return new ExecutionResult(visibleOutput, visibleCommand, exitCode);
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        private static async Task<string> PrepareScriptFile(Script script, ReadOnlyCollection<InvocationArgument> args, string[] environmentVariables,
            string tempFilePath)
        {
            var argDict = args.ToDictionary(arg => arg.Name);
            var scriptText = ReplaceVariables(script, argDict);
            var finalText = string.Join('\n', environmentVariables) + "\n" + scriptText;
            await File.WriteAllTextAsync(tempFilePath, finalText);
            return scriptText;
        }

        private ProcessStartInfo PrepareStartInfo(string[] environmentVariables, string tempFilePath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _executionOptions.Shell,
                Arguments = _executionOptions.UseWslPath ? GetWslPath(tempFilePath) : tempFilePath
            };
            foreach (var environmentVariable in environmentVariables)
            {
                var kvp = environmentVariable.Split('=');
                startInfo.EnvironmentVariables.Add(kvp[0].Trim(), kvp[1].Trim());
            }

            return startInfo;
        }

        private static string GetWslPath(string path)
        {
            return Path.GetFullPath(path).Replace("C:\\", "/mnt/c/").Replace("\\", "/");
        }

        private static string ReplaceVariables(Script script, Dictionary<string, InvocationArgument> argDict)
        {
            return Regex.Replace(script.Command, @"\$\(([^)]+)\)", match =>
            {
                var key = match.Groups[1].Value;
                if (argDict.TryGetValue(key, out var value))
                    return value.Value.ToString()!;
                return match.Value;
            });
        }

        private string HideSecrets(string text, IEnumerable<InvocationArgument> args)
        {
            var sb = new StringBuilder(text);
            foreach (var secret in args.Where(arg => arg.Secret))
            {
                var valueString = secret.Value.ToString()!;
                sb.Replace(valueString, new string('*', valueString.Length));
            }

            return sb.ToString();
        }
    }
}
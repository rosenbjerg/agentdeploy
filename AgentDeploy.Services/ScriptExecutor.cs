using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Application.Parser.Models;
using Instances;

namespace AgentDeploy.Application.Parser
{
    public record ExecutionResult(string Output, string Command, int ExitCode);
    public class ScriptExecutor
    {
        public async Task<ExecutionResult> Execute(Script script, ReadOnlyCollection<InvocationArgument> args, string[] env)
        {
            var argDict = args.ToDictionary(arg => arg.Name);
            var scriptText = ReplaceVariables(script, argDict);

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.sh");
            try
            {
                await File.WriteAllTextAsync(tempFilePath, scriptText);
                var wslPath = Path.GetFullPath(tempFilePath).Replace("C:\\", "/mnt/c/").Replace("\\", "/");
                var instance = new Instance("bash", wslPath);
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
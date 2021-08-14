using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.Services.Scripts
{
    public static class ScriptInvocationParser
    {
        private static readonly Regex VariableRegex = new("^([a-zA-Z0-9-_]+)=(.*)$", RegexOptions.Compiled);
        
        public static ParsedScriptInvocation Parse(ScriptInvocation scriptInvocation)
        {
            var failed = new HashSet<InvocationArgumentError>();
            var result =  new ParsedScriptInvocation
            {
                ScriptName = scriptInvocation.ScriptName,
                WebsocketSessionId = scriptInvocation.WebsocketSessionId,
                Variables = SplitVariables(scriptInvocation.Variables, scriptInvocation.SecretVariables, failed),
                EnvironmentVariables = ParseEnvironmentVariables(scriptInvocation.EnvironmentVariables, failed),
                Files = ParseFormFiles(scriptInvocation.Files, failed)
            };
            if (failed.Any()) throw new FailedInvocationValidationException(failed);

            return result;
        }

        private static Dictionary<string, ScriptInvocationVariable> SplitVariables(string[] variables,
            string[] secretVariables, ISet<InvocationArgumentError> invocationArgumentErrors)
        {
            var result = new Dictionary<string, ScriptInvocationVariable>();
            
            foreach (var variable in variables)
            {
                var scriptInvocationVariable = ParseScriptInvocationVariable(variable, false);
                if (scriptInvocationVariable == null)
                    invocationArgumentErrors.Add(new InvocationArgumentError(variable, $"Invalid variable provided. Must match: {VariableRegex}"));
                else if (!result.TryAdd(scriptInvocationVariable.Key, scriptInvocationVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptInvocationVariable.Key, "Variable with same key already provided"));
            }
            
            foreach (var secretVariable in secretVariables)
            {
                var scriptInvocationVariable = ParseScriptInvocationVariable(secretVariable, true);
                if (scriptInvocationVariable == null)
                    invocationArgumentErrors.Add(new InvocationArgumentError(secretVariable, $"Invalid secret variable provided. Must match: {VariableRegex}"));
                else if (!result.TryAdd(scriptInvocationVariable.Key, scriptInvocationVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptInvocationVariable.Key, "Secret variable with same key already provided"));
            }

            return result;
        }

        private static ScriptInvocationVariable? ParseScriptInvocationVariable(string value, bool secret)
        {
            var match = VariableRegex.Match(value);
            if (!match.Success)
                return null;
            
            return new ScriptInvocationVariable(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim(), secret);
        }
        
        private static ScriptEnvironmentVariable[] ParseEnvironmentVariables(string[] values,
            ICollection<InvocationArgumentError> invocationArgumentErrors)
        {
            var result = new Dictionary<string, ScriptEnvironmentVariable>();
            foreach (var value in values)
            {
                var match = VariableRegex.Match(value);
                if (!match.Success)
                {
                    invocationArgumentErrors.Add(new InvocationArgumentError(value, $"Invalid environment variable provided. Must match: {VariableRegex}"));
                    continue;
                }
                var scriptEnvironmentVariable = new ScriptEnvironmentVariable(match.Groups[1].Value.Trim(), match.Groups[2].Value.Trim());
                if (!result.TryAdd(scriptEnvironmentVariable.Key, scriptEnvironmentVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptEnvironmentVariable.Key, "Environment variable with same key already provided"));
            }
            return result.Values.ToArray();
        }

        private static Dictionary<string, ScriptInvocationFile> ParseFormFiles(IFormFile[] formFiles,
            ISet<InvocationArgumentError> invocationArgumentErrors)
        {
            var result = new Dictionary<string, ScriptInvocationFile>();

            foreach (var formFile in formFiles)
            {
                var match = VariableRegex.Match(formFile.FileName);
                if (!match.Success)
                {
                    invocationArgumentErrors.Add(new InvocationArgumentError(formFile.FileName, $"Invalid file name. Must match: {VariableRegex}"));
                    continue;
                }
                var scriptInvocationFile = new ScriptInvocationFile
                {
                    Key = match.Groups[1].Value.Trim(),
                    FileName = match.Groups[2].Value.Trim(),
                    FileSize = formFile.Length,
                    Read = formFile.OpenReadStream
                };
                if (!result.TryAdd(scriptInvocationFile.Key, scriptInvocationFile))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptInvocationFile.Key, "File with same key already provided"));
            }

            return result;
        }
    }
}
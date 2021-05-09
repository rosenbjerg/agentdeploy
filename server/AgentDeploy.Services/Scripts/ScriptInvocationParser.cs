using System.Collections.Generic;
using System.Linq;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.Services.Scripts
{
    public sealed class ScriptInvocationParser : IScriptInvocationParser
    {
        public ParsedScriptInvocation Parse(ScriptInvocation scriptInvocation)
        {
            var failed = new List<InvocationArgumentError>();
            var result =  new ParsedScriptInvocation
            {
                ScriptName = scriptInvocation.ScriptName,
                WebsocketSessionId = scriptInvocation.WebsocketSessionId,
                Variables = SplitVariables(scriptInvocation.Variables, scriptInvocation.SecretVariables, failed),
                EnvironmentVariables = ParseEnvironmentVariables(scriptInvocation.EnvironmentVariables, failed),
                Files = ParseFormFiles(scriptInvocation.Files, failed)
            };
            if (failed.Any()) throw new InvalidInvocationArgumentsException(failed);

            return result;
        }

        private Dictionary<string, ScriptInvocationVariable> SplitVariables(string[] variables,
            string[] secretVariables, List<InvocationArgumentError> invocationArgumentErrors)
        {
            var result = new Dictionary<string, ScriptInvocationVariable>();
            
            foreach (var variable in variables)
            {
                var scriptInvocationVariable = ParseScriptInvocationVariable(variable, false);
                if (!result.TryAdd(scriptInvocationVariable.Key, scriptInvocationVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptInvocationVariable.Key, "Variable with same key already provided"));
            }
            
            foreach (var secretVariable in secretVariables)
            {
                var scriptInvocationVariable = ParseScriptInvocationVariable(secretVariable, true);
                if (!result.TryAdd(scriptInvocationVariable.Key, scriptInvocationVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptInvocationVariable.Key, "Secret variable with same key already provided"));
            }

            return result;
        }

        private ScriptInvocationVariable ParseScriptInvocationVariable(string value, bool secret, char splitCharacter = '=')
        {
            var split = value.Split(splitCharacter);
            return new ScriptInvocationVariable(split[0].Trim(), split[1].Trim(), secret);
        }
        
        private ScriptEnvironmentVariable[] ParseEnvironmentVariables(string[] values,
            List<InvocationArgumentError> invocationArgumentErrors, char splitCharacter = '=')
        {
            var result = new Dictionary<string, ScriptEnvironmentVariable>();
            foreach (var value in values)
            {
                var split = value.Split(splitCharacter);
                var scriptEnvironmentVariable = new ScriptEnvironmentVariable(split[0].Trim(), split[1].Trim());
                if (!result.TryAdd(scriptEnvironmentVariable.Key, scriptEnvironmentVariable))
                    invocationArgumentErrors.Add(new InvocationArgumentError(scriptEnvironmentVariable.Key, "Environment variable with same key already provided"));
            }
            return result.Values.ToArray();
        }

        private Dictionary<string, ScriptInvocationFile> ParseFormFiles(IFormFile[] formFiles,
            List<InvocationArgumentError> invocationArgumentErrors, char splitCharacter = '=')
        {
            var result = new Dictionary<string, ScriptInvocationFile>();

            foreach (var formFile in formFiles)
            {
                var split = formFile.FileName.Split(splitCharacter);
                var scriptInvocationFile = new ScriptInvocationFile
                {
                    Key = split[0].Trim(),
                    FileName = split[1].Trim(),
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
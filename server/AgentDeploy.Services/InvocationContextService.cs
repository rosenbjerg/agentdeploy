using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Script;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AgentDeploy.Services
{
    public class InvocationContextService
    {
        private readonly IOperationContext _operationContext;
        private readonly ScriptReader _scriptReader;

        public InvocationContextService(IOperationContext operationContext, ScriptReader scriptReader)
        {
            _operationContext = operationContext;
            _scriptReader = scriptReader;
        }
        
        public async Task<ScriptInvocationContext?> Build(ParsedScriptInvocation scriptInvocation)
        {
            var script = await _scriptReader.Load(scriptInvocation.ScriptName);
            if (script == null)
                return null;
            
            var failed = new List<InvocationArgumentError>();
            var acceptedVariables = new List<AcceptedScriptInvocationArgument>();
            var acceptedFiles = new List<AcceptedScriptInvocationFile>();
            
            var commandConstraints = GetScriptConstraints(scriptInvocation.ScriptName);
            foreach (var inputVariable in script.Variables)
            {
                var invocationValue = ValidateInputVariables(scriptInvocation.Variables, inputVariable, commandConstraints, failed);
                if (invocationValue == null) continue;
                acceptedVariables.Add(new AcceptedScriptInvocationArgument(inputVariable.Key, inputVariable.Value.Type, invocationValue.Value, invocationValue.Secret || inputVariable.Value.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var providedFile = ValidateFileInput(scriptInvocation.Files, inputFile, failed);
                if (providedFile == null) continue;
                acceptedFiles.Add(new AcceptedScriptInvocationFile(inputFile.Key, Path.GetFileName(providedFile.FileName), providedFile.Read));
            }
            
            if (failed.Any()) throw new InvalidInvocationArgumentsException(failed);

            return new ScriptInvocationContext
            {
                Script = script,
                Arguments = acceptedVariables,
                Files = acceptedFiles.ToArray(),
                EnvironmentVariables = scriptInvocation.EnvironmentVariables,
                SecureShellOptions = commandConstraints?.Ssh ?? _operationContext.Token.Ssh,
                WebSocketSessionId = scriptInvocation.WebsocketSessionId
            };
        }

        private static Guid? ExtractWebsocketToken(IFormCollection formCollection)
        {
            if (formCollection.TryGetValue("websocket-session-id", out var sessionIdString) &&
                Guid.TryParse(sessionIdString, out var sessionId))
                return sessionId;
            return null;
        }

        private static ScriptInvocationVariable? ValidateInputVariables(Dictionary<string, ScriptInvocationVariable> scriptInvocationVariables, KeyValuePair<string, ScriptArgumentDefinition> inputVariable,
            ScriptAccessDeclaration? scriptAccess, List<InvocationArgumentError> failed)
        {
            if (!scriptInvocationVariables.TryGetValue(inputVariable.Key, out var invocationValue))
            {
                if (scriptAccess != null &&
                    scriptAccess.LockedVariables.TryGetValue(inputVariable.Key, out var lockedValue))
                {
                    invocationValue = new ScriptInvocationVariable(inputVariable.Key, lockedValue, false);
                }
                else if (inputVariable.Value.DefaultValue != null)
                {
                    invocationValue = new ScriptInvocationVariable(inputVariable.Key, inputVariable.Value.DefaultValue, false);
                }
                else
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, "No value provided"));
                    return null;
                }
            }
            else if (scriptAccess != null && scriptAccess.LockedVariables.ContainsKey(inputVariable.Key))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, "Variable is locked and can not be provided"));
                return null;
            }

            if (inputVariable.Value.Regex != null && !Regex.IsMatch(invocationValue.Value, inputVariable.Value.Regex))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass command regex validation ({inputVariable.Value.Regex})"));
                return null;
            }

            if (scriptAccess != null &&
                scriptAccess.VariableContraints.TryGetValue(inputVariable.Key, out var profileArgumentConstraint) &&
                !Regex.IsMatch(invocationValue.Value, profileArgumentConstraint))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                return null;
            }

            if (inputVariable.Value.Type == ScriptArgumentType.Integer && !IntegerRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({IntegerRegex})"));
                return null;
            }

            if (inputVariable.Value.Type == ScriptArgumentType.Float && !FloatRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({FloatRegex})"));
                return null;
            }

            return invocationValue;
        }

        private static ScriptInvocationFile? ValidateFileInput(Dictionary<string, ScriptInvocationFile> scriptInvocationFiles, KeyValuePair<string, ScriptFileArgument> inputFile, List<InvocationArgumentError> failed)
        {
            if (!scriptInvocationFiles.TryGetValue(inputFile.Key, out var scriptFileArgument))
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, "No file provided"));
                return null;
            }

            if (scriptFileArgument.FileSize < inputFile.Value.MinSize)
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too few bytes (min. {inputFile.Value.MinSize})"));
                return null;
            }

            if (scriptFileArgument.FileSize > inputFile.Value.MaxSize)
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too many bytes (max. {inputFile.Value.MaxSize})"));
                return null;
            }

            var ext = Path.GetExtension(scriptFileArgument.FileName).TrimStart('.').ToLowerInvariant();
            if (inputFile.Value.AcceptedExtensions != null && !inputFile.Value.AcceptedExtensions.Contains(ext))
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File extension '{ext}' is not accepted (accepted: {string.Join(", ", inputFile.Value.AcceptedExtensions)})"));
                return null;
            }

            return scriptFileArgument;
        }

        private ScriptAccessDeclaration? GetScriptConstraints(string command)
        {
            if (_operationContext.Token.AvailableCommands != null)
            {
                if (!_operationContext.Token.AvailableCommands.TryGetValue(command, out var commandConstraints))
                {
                    throw new InvalidInvocationArgumentsException(new List<InvocationArgumentError>
                    {
                        new InvocationArgumentError(command, "Command not allowed")
                    });
                }
                
                return commandConstraints;
            }

            return null;
        }

        private static Regex IntegerRegex = new("^\\d+$", RegexOptions.Compiled);
        private static Regex FloatRegex = new("^\\d+\\.\\d+$", RegexOptions.Compiled);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AgentDeploy.Services
{
    public class ExecutionContextService
    {
        private readonly IOperationContext _operationContext;
        private readonly CommandReader _commandReader;

        public ExecutionContextService(IOperationContext operationContext, CommandReader commandReader)
        {
            _operationContext = operationContext;
            _commandReader = commandReader;
        }
        
        public async Task<ScriptExecutionContext?> Build(string command, IFormCollection formCollection)
        {
            var script = await _commandReader.Load(command);
            if (script == null)
                return null;
            
            var failed = new List<InvocationArgumentError>();
            var accepted = new List<InvocationArgument>();
            var acceptedFiles = new List<InvocationFile>();
            var commandConstraints = GetCommandConstraints(command);
            var rawInvocationArguments = ParseRawInvocationArguments(formCollection);
            foreach (var inputVariable in script.Variables)
            {
                var invocationValue = ValidateInputVariables(rawInvocationArguments, inputVariable, commandConstraints, failed);
                if (invocationValue == null) continue;
                accepted.Add(new InvocationArgument(inputVariable.Key, inputVariable.Value.Type, invocationValue.Value, invocationValue.Secret || inputVariable.Value.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var providedFile = ValidateFileInput(formCollection, inputFile, failed);
                if (providedFile == null) continue;
                acceptedFiles.Add(new InvocationFile(inputFile.Key, Path.GetFileName(providedFile.FileName), providedFile.OpenReadStream));
            }
            
            if (failed.Any()) throw new InvalidInvocationArgumentsException(failed);

            var sessionId = ExtractWebsocketToken(formCollection);
            var environmentVariables = formCollection.Where(e => e.Key == "environment").SelectMany(e => e.Value).Select(env => env.Trim()).ToArray();
            return new ScriptExecutionContext
            {
                Script = script,
                Arguments = accepted,
                Files = acceptedFiles.ToArray(),
                EnvironmentVariables = environmentVariables,
                SecureShellOptions = commandConstraints?.Ssh,
                WebSocketSessionId = sessionId
            };
        }

        private static Guid? ExtractWebsocketToken(IFormCollection formCollection)
        {
            if (formCollection.TryGetValue("websocket-session-id", out var sessionIdString) &&
                Guid.TryParse(sessionIdString, out var sessionId))
                return sessionId;
            return null;
        }

        private static RawInvocationArgument? ValidateInputVariables(Dictionary<string, RawInvocationArgument> rawInvocationArguments, KeyValuePair<string, ScriptArgument> inputVariable,
            ConstrainedCommand? commandConstraints, List<InvocationArgumentError> failed)
        {
            if (!rawInvocationArguments.TryGetValue(inputVariable.Key, out var invocationValue))
            {
                if (commandConstraints != null &&
                    commandConstraints.LockedVariables.TryGetValue(inputVariable.Key, out var lockedValue))
                {
                    invocationValue = new RawInvocationArgument(inputVariable.Key, lockedValue, false);
                }
                else if (inputVariable.Value.DefaultValue != null)
                {
                    invocationValue = new RawInvocationArgument(inputVariable.Key, inputVariable.Value.DefaultValue, false);
                }
                else
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, "No value provided"));
                    return null;
                }
            }
            else if (commandConstraints != null && commandConstraints.LockedVariables.ContainsKey(inputVariable.Key))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, "Variable is locked and can not be provided"));
                return null;
            }

            if (inputVariable.Value.Regex != null && !Regex.IsMatch(invocationValue.Value, inputVariable.Value.Regex))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass command regex validation ({inputVariable.Value.Regex})"));
                return null;
            }

            if (commandConstraints != null &&
                commandConstraints.VariableContraints.TryGetValue(inputVariable.Key, out var profileArgumentConstraint) &&
                !Regex.IsMatch(invocationValue.Value, profileArgumentConstraint))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                return null;
            }

            if (inputVariable.Value.Type == ArgumentType.Integer && !IntegerRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({IntegerRegex})"));
                return null;
            }

            if (inputVariable.Value.Type == ArgumentType.Float && !FloatRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({FloatRegex})"));
                return null;
            }

            return invocationValue;
        }

        private static IFormFile? ValidateFileInput(IFormCollection form, KeyValuePair<string, ScriptFileArgument> inputFile, List<InvocationArgumentError> failed)
        {
            var providedFile = form.Files.GetFile(inputFile.Key);
            if (providedFile == null)
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, "No file provided"));
                return null;
            }

            if (providedFile.Length < inputFile.Value.MinSize)
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too few bytes (min. {inputFile.Value.MinSize})"));
                return null;
            }

            if (providedFile.Length > inputFile.Value.MaxSize)
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too many bytes (max. {inputFile.Value.MaxSize})"));
                return null;
            }

            var ext = Path.GetExtension(providedFile.FileName).TrimStart('.').ToLowerInvariant();
            if (inputFile.Value.AcceptedExtensions != null && !inputFile.Value.AcceptedExtensions.Contains(ext))
            {
                failed.Add(new InvocationArgumentError(inputFile.Key, $"File extension '{ext}' is not accepted (accepted: {string.Join(", ", inputFile.Value.AcceptedExtensions)})"));
                return null;
            }

            return providedFile;
        }

        private ConstrainedCommand? GetCommandConstraints(string command)
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
        
        private static Dictionary<string, RawInvocationArgument> ParseRawInvocationArguments(IFormCollection formCollection)
        {
            var rawInvocationArguments = new List<RawInvocationArgument>();
            rawInvocationArguments.AddRange(ParseRawInvocationVariables(formCollection.Where(kvp => kvp.Key == "variable"), false));
            rawInvocationArguments.AddRange(ParseRawInvocationVariables(formCollection.Where(kvp => kvp.Key == "secretVariable"), true));
            return rawInvocationArguments.ToDictionary(ia => ia.Name);
        }

        private static IEnumerable<RawInvocationArgument> ParseRawInvocationVariables(IEnumerable<KeyValuePair<string, StringValues>> keyValuePairs, bool secret)
        {
            return from kvp in keyValuePairs
                from stringValue in kvp.Value
                select stringValue.Split('=')
                into split
                select new RawInvocationArgument(split[0].Trim(), split[1].Trim(), secret);
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;
using AgentDeploy.Services.Scripts;

namespace AgentDeploy.Services
{
    public class InvocationContextService : IInvocationContextService
    {
        private readonly IOperationContext _operationContext;
        private readonly IScriptReader _scriptReader;

        public InvocationContextService(IOperationContext operationContext, IScriptReader scriptReader)
        {
            _operationContext = operationContext;
            _scriptReader = scriptReader;
        }
        
        public async Task<ScriptInvocationContext?> Build(ParsedScriptInvocation scriptInvocation)
        {
            var script = HasAccessToScript(scriptInvocation.ScriptName)
                ? await _scriptReader.Load(scriptInvocation.ScriptName)
                : null;
            if (script == null)
                return null;
            
            var failed = new List<InvocationArgumentError>();
            var acceptedVariables = new List<AcceptedScriptInvocationArgument>();
            var acceptedFiles = new List<AcceptedScriptInvocationFile>();

            var scriptAccessDeclaration = _operationContext.Token!.AvailableScripts != null
                ? _operationContext.Token.AvailableScripts[scriptInvocation.ScriptName] ?? new ScriptAccessDeclaration()
                : null;
            foreach (var inputVariable in script.Variables)
            {
                var scriptVariableDefinition = inputVariable.Value ?? new ScriptVariableDefinition();
                var invocationValue = ValidateInputVariables(scriptInvocation.Variables, inputVariable.Key, scriptVariableDefinition, scriptAccessDeclaration, failed);
                if (invocationValue == null) continue;
                acceptedVariables.Add(new AcceptedScriptInvocationArgument(inputVariable.Key, scriptVariableDefinition.Type, invocationValue.Value, invocationValue.Secret || scriptVariableDefinition.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var scriptFileArgument = inputFile.Value ?? new ScriptFileDefinition();
                var providedFile = ValidateFileInput(scriptInvocation.Files, inputFile.Key, scriptFileArgument, failed);
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
                SecureShellOptions = scriptAccessDeclaration?.Ssh ?? _operationContext.Token.Ssh,
                WebSocketSessionId = scriptInvocation.WebsocketSessionId
            };
        }

        private static ScriptInvocationVariable? ValidateInputVariables(Dictionary<string, ScriptInvocationVariable> scriptInvocationVariables, string scriptVariableKey, ScriptVariableDefinition scriptVariableDefinition,
            ScriptAccessDeclaration? scriptAccess, List<InvocationArgumentError> failed)
        {
            if (!scriptInvocationVariables.TryGetValue(scriptVariableKey, out var invocationValue))
            {
                if (scriptAccess != null && scriptAccess.LockedVariables.TryGetValue(scriptVariableKey, out var lockedValue))
                {
                    invocationValue = new ScriptInvocationVariable(scriptVariableKey, lockedValue, false);
                }
                else if (scriptVariableDefinition.DefaultValue != null)
                {
                    invocationValue = new ScriptInvocationVariable(scriptVariableKey, scriptVariableDefinition.DefaultValue, false);
                }
                else
                {
                    failed.Add(new InvocationArgumentError(scriptVariableKey, "No value provided"));
                    return null;
                }
            }
            else if (scriptAccess != null && scriptAccess.LockedVariables.ContainsKey(scriptVariableKey))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, "Variable is locked and can not be provided"));
                return null;
            }

            if (scriptVariableDefinition.Regex != null && !Regex.IsMatch(invocationValue.Value, scriptVariableDefinition.Regex))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass script regex validation ({scriptVariableDefinition.Regex})"));
                return null;
            }

            if (scriptAccess != null &&
                scriptAccess.VariableContraints.TryGetValue(scriptVariableKey, out var profileArgumentConstraint) &&
                !Regex.IsMatch(invocationValue.Value, profileArgumentConstraint))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                return null;
            }

            if (scriptVariableDefinition.Type == ScriptArgumentType.Integer && !IntegerRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass type validation ({IntegerRegex})"));
                return null;
            }

            if (scriptVariableDefinition.Type == ScriptArgumentType.Float && !FloatRegex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass type validation ({FloatRegex})"));
                return null;
            }

            return invocationValue;
        }

        private static ScriptInvocationFile? ValidateFileInput(Dictionary<string, ScriptInvocationFile> scriptInvocationFiles, string scriptFileKey, ScriptFileDefinition scriptFileDefinition, List<InvocationArgumentError> failed)
        {
            if (!scriptInvocationFiles.TryGetValue(scriptFileKey, out var scriptFileArgument))
            {
                failed.Add(new InvocationArgumentError(scriptFileKey, "No file provided"));
                return null;
            }

            if (scriptFileArgument.FileSize < scriptFileDefinition.MinSize)
            {
                failed.Add(new InvocationArgumentError(scriptFileKey, $"File contains too few bytes (min. {scriptFileDefinition.MinSize})"));
                return null;
            }

            if (scriptFileArgument.FileSize > scriptFileDefinition.MaxSize)
            {
                failed.Add(new InvocationArgumentError(scriptFileKey, $"File contains too many bytes (max. {scriptFileDefinition.MaxSize})"));
                return null;
            }

            var ext = Path.GetExtension(scriptFileArgument.FileName).TrimStart('.').ToLowerInvariant();
            if (scriptFileDefinition.AcceptedExtensions != null && !scriptFileDefinition.AcceptedExtensions.Contains(ext))
            {
                failed.Add(new InvocationArgumentError(scriptFileKey, $"File extension '{ext}' is not accepted (accepted: {string.Join(", ", scriptFileDefinition.AcceptedExtensions)})"));
                return null;
            }

            return scriptFileArgument;
        }

        private bool HasAccessToScript(string scriptName)
        {
            return _operationContext.Token.AvailableScripts == null || _operationContext.Token.AvailableScripts.ContainsKey(scriptName);
        }

        private static Regex IntegerRegex = new("^\\d+$", RegexOptions.Compiled);
        private static Regex FloatRegex = new("^\\d+\\.\\d+$", RegexOptions.Compiled);
    }
}
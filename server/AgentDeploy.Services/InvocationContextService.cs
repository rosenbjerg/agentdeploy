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
    public interface ITypeValidator
    {
        
    }

    public sealed class RegexTypeValidator
    {
        
    }
    
    public sealed class InvocationContextService : IInvocationContextService
    {
        private static readonly Dictionary<ScriptArgumentType, Regex?> TypeValidation = new()
        {
            { ScriptArgumentType.Integer, new Regex("^\\d+$", RegexOptions.Compiled) },
            { ScriptArgumentType.Decimal, new Regex("^\\d+\\.\\d+$", RegexOptions.Compiled) },
            { ScriptArgumentType.Boolean, new Regex("^true|false$", RegexOptions.Compiled) },
            { ScriptArgumentType.FQDN, new Regex("^(?=^.{4,253}$)(^((?!-)[a-zA-Z0-9-]{0,62}[a-zA-Z0-9]\\.)+[a-zA-Z]{2,63}$)$", RegexOptions.Compiled) },
            { ScriptArgumentType.IPv4, new Regex("^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}↵(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled) },
            { ScriptArgumentType.String, null },
        };
        
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
                ? await _scriptReader.Load(scriptInvocation.ScriptName, _operationContext.OperationCancelled)
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
                acceptedVariables.Add(new AcceptedScriptInvocationArgument(inputVariable.Key, invocationValue.Value, invocationValue.Secret || scriptVariableDefinition.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var scriptFileArgument = inputFile.Value ?? new ScriptFileDefinition();
                var providedFile = ValidateFileInput(scriptInvocation.Files, inputFile.Key, scriptFileArgument, failed);
                if (providedFile == null) continue;
                acceptedFiles.Add(new AcceptedScriptInvocationFile(inputFile.Key, Path.GetFileName(providedFile.FileName), scriptFileArgument.FilePreprocessing, providedFile.Read));
            }
            
            if (failed.Any()) throw new FailedInvocationValidationException(failed);

            return new ScriptInvocationContext
            {
                Script = script,
                Arguments = acceptedVariables,
                Files = acceptedFiles.ToArray(),
                EnvironmentVariables = scriptInvocation.EnvironmentVariables,
                SecureShellOptions = scriptAccessDeclaration?.Ssh ?? _operationContext.Token.Ssh,
                WebSocketSessionId = scriptInvocation.WebsocketSessionId,
                CorrelationId = _operationContext.CorrelationId
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

            var regex = TypeValidation[scriptVariableDefinition.Type];
            if (regex != null && !regex.IsMatch(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass type validation ({regex})"));
                return null;
            }
            
            return invocationValue;
        }

        private static ScriptInvocationFile? ValidateFileInput(Dictionary<string, ScriptInvocationFile> scriptInvocationFiles, string scriptFileKey, ScriptFileDefinition scriptFileDefinition, List<InvocationArgumentError> failed)
        {
            if (!scriptInvocationFiles.TryGetValue(scriptFileKey, out var scriptFileArgument))
            {
                if (scriptFileDefinition.Optional)
                {
                    return new ScriptInvocationFile
                    {
                        Key = scriptFileKey,
                    };
                }
                else
                {
                    failed.Add(new InvocationArgumentError(scriptFileKey, "No file provided"));
                    return null;
                }
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
    }
}
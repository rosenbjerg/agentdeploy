using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Models.Tokens;

namespace AgentDeploy.Services.TypeValidation
{
    public sealed class TypeValidationService : ITypeValidationService
    {
        private readonly Dictionary<ScriptArgumentType, ITypeValidator> _typeValidators = new()
        {
            { ScriptArgumentType.Integer, new RegexTypeValidator("^\\d+$") },
            { ScriptArgumentType.Decimal, new RegexTypeValidator("^\\d+\\.\\d+$") },
            { ScriptArgumentType.Boolean, new RegexTypeValidator("^true|false$") },
            { ScriptArgumentType.Hostname, new UriTypeValidator(UriHostNameType.Basic, UriHostNameType.Dns, UriHostNameType.IPv4, UriHostNameType.IPv6) },
            { ScriptArgumentType.DnsName, new UriTypeValidator(UriHostNameType.Dns) },
            { ScriptArgumentType.IP, new UriTypeValidator(UriHostNameType.IPv4, UriHostNameType.IPv6) },
            { ScriptArgumentType.IPv4, new UriTypeValidator(UriHostNameType.IPv4) },
            { ScriptArgumentType.IPv6, new UriTypeValidator(UriHostNameType.IPv6) },
            { ScriptArgumentType.Email, new MailAddressTypeValidator() },
            { ScriptArgumentType.String, new NoopValidator() },
        };

        public (List<AcceptedScriptInvocationArgument> AcceptedVariables, List<AcceptedScriptInvocationFile> AcceptedFiles) Validate(ParsedScriptInvocation scriptInvocation, Script script, ScriptAccessDeclaration? scriptAccessDeclaration)
        {
            var failed = new List<InvocationArgumentError>();
            var acceptedVariables = new List<AcceptedScriptInvocationArgument>();
            var acceptedFiles = new List<AcceptedScriptInvocationFile>();
            
            foreach (var inputVariable in script.Variables)
            {
                var scriptVariableDefinition = inputVariable.Value ?? new ScriptVariableDefinition();
                var invocationValue = ValidateVariable(scriptInvocation.Variables, inputVariable.Key, scriptVariableDefinition, scriptAccessDeclaration, failed);
                if (invocationValue == null) continue;
                
                acceptedVariables.Add(new AcceptedScriptInvocationArgument(inputVariable.Key, invocationValue.Value,
                    invocationValue.Secret || scriptVariableDefinition.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var scriptFileArgument = inputFile.Value ?? new ScriptFileDefinition();
                var providedFile =
                    ValidateFile(scriptInvocation.Files, inputFile.Key, scriptFileArgument, failed);
                if (providedFile == null) continue;
                
                acceptedFiles.Add(new AcceptedScriptInvocationFile(inputFile.Key, Path.GetFileName(providedFile.FileName),
                    scriptFileArgument.FilePreprocessing, providedFile.Read));
            }

            if (failed.Any()) throw new FailedInvocationValidationException(failed);

            return (acceptedVariables, acceptedFiles);
        }

        private ScriptInvocationVariable? ValidateVariable(Dictionary<string, ScriptInvocationVariable> scriptInvocationVariables, string scriptVariableKey, ScriptVariableDefinition scriptVariableDefinition,
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
                scriptAccess.VariableConstraints.TryGetValue(scriptVariableKey, out var profileArgumentConstraint) &&
                !Regex.IsMatch(invocationValue.Value, profileArgumentConstraint))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                return null;
            }

            var validator = _typeValidators[scriptVariableDefinition.Type];
            if (!validator.IsValid(invocationValue.Value))
            {
                failed.Add(new InvocationArgumentError(scriptVariableKey, $"Provided value does not pass type validation ({validator})"));
                return null;
            }
            
            return invocationValue;
        }

        private ScriptInvocationFile? ValidateFile(Dictionary<string, ScriptInvocationFile> scriptInvocationFiles, string scriptFileKey, ScriptFileDefinition scriptFileDefinition, List<InvocationArgumentError> failed)
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
    }
}
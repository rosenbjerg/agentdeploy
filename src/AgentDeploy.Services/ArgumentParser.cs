using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AgentDeploy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AgentDeploy.Services
{
    public class ArgumentParser
    {
        public ScriptExecutionContext Parse(IFormCollection formCollection, Script script, ConstrainedCommand profile)
        {
            var failed = new List<InvocationArgumentError>();
            var accepted = new List<InvocationArgument>();
            var acceptedFiles = new List<InvocationFile>();
            
            var rawInvocationArguments = ParseRawInvocationArguments(formCollection);
            foreach (var inputVariable in script.Variables)
            {
                if (!rawInvocationArguments.TryGetValue(inputVariable.Key, out var invocationValue))
                {
                    if (profile.LockedVariables.TryGetValue(inputVariable.Key, out var lockedValue))
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
                        continue;
                    }
                }
                else if (profile.LockedVariables.ContainsKey(inputVariable.Key))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, "Variable is locked and can not be provided"));
                    continue;
                }
                
                if (inputVariable.Value.Regex != null && !Regex.IsMatch(invocationValue.Value, inputVariable.Value.Regex))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass command regex validation ({inputVariable.Value.Regex})"));
                    continue;
                }

                if (profile.VariableContraints.TryGetValue(inputVariable.Key, out var profileArgumentConstraint) && !Regex.IsMatch(invocationValue.Value, profileArgumentConstraint))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                    continue;
                }

                if (inputVariable.Value.Type == ArgumentType.Integer && !IntegerRegex.IsMatch(invocationValue.Value))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({IntegerRegex})"));
                    continue;
                }
                if (inputVariable.Value.Type == ArgumentType.Float && !FloatRegex.IsMatch(invocationValue.Value))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass type validation ({FloatRegex})"));
                    continue;
                }
                
                accepted.Add(new InvocationArgument(invocationValue.Name, inputVariable.Value.Type, invocationValue.Value, invocationValue.Secret));
            }

            foreach (var inputFile in script.Files)
            {
                var providedFile = formCollection.Files.GetFile(inputFile.Key);
                if (providedFile == null)
                {
                    failed.Add(new InvocationArgumentError(inputFile.Key, "No file provided"));
                    continue;
                }
                
                if (providedFile.Length < inputFile.Value.MinSize)
                {
                    failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too few bytes (min. {inputFile.Value.MinSize})"));
                    continue;
                }
                if (providedFile.Length > inputFile.Value.MaxSize)
                {
                    failed.Add(new InvocationArgumentError(inputFile.Key, $"File contains too many bytes (max. {inputFile.Value.MaxSize})"));
                    continue;
                }

                var ext = Path.GetExtension(providedFile.FileName).TrimStart('.').ToLowerInvariant();
                if (inputFile.Value.AcceptedExtensions != null && !inputFile.Value.AcceptedExtensions.Contains(ext))
                {
                    failed.Add(new InvocationArgumentError(inputFile.Key, $"File extension '{ext}' is not accepted (accepted: {string.Join(", ", inputFile.Value.AcceptedExtensions)})"));
                    continue;
                }

                acceptedFiles.Add(new InvocationFile(inputFile.Key, Path.GetFileName(providedFile.FileName), providedFile.OpenReadStream));
            }
            
            if (failed.Any())
                throw new InvalidInvocationArgumentsException(failed);
            
            var environmentVariables = formCollection.Where(e => e.Key == "environment").SelectMany(e => e.Value).Select(env => env.Trim()).ToArray();

            return new ScriptExecutionContext(accepted, acceptedFiles.ToArray(), environmentVariables, profile.Ssh);
        }

        private static Regex IntegerRegex = new Regex("^\\d+$", RegexOptions.Compiled);
        private static Regex FloatRegex = new Regex("^\\d+\\.\\d+$", RegexOptions.Compiled);
        
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
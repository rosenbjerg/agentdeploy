using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using AgentDeploy.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace AgentDeploy.Services
{
    public record InvocationArgument(string Name, ArgumentType Type, object Value, bool Secret);
    public record InvocationArgumentError(string Name, string Error);
    public record RawInvocationArgument(string Name, string Value, bool Secret);
    public class ArgumentParser
    {
        public (ReadOnlyCollection<InvocationArgument> accepted, string[] enviromnentVariables) Parse(
            IFormCollection formCollection, Dictionary<string, ScriptArgument> scriptArguments, Dictionary<string, string> profileArgumentConstraints)
        {
            var failed = new List<InvocationArgumentError>();
            var accepted = new List<InvocationArgument>();
            
            var rawInvocationArguments = ParseRawInvocationArguments(formCollection);
            foreach (var inputVariable in scriptArguments)
            {
                if (!rawInvocationArguments.TryGetValue(inputVariable.Key, out var arg))
                {
                    if (inputVariable.Value.DefaultValue == null)
                    {
                        failed.Add(new InvocationArgumentError(inputVariable.Key, "No value provided"));
                        continue;
                    }

                    arg = new RawInvocationArgument(inputVariable.Key, inputVariable.Value.DefaultValue, false);
                }
                
                if (inputVariable.Value.Regex != null && !Regex.IsMatch(arg.Value, inputVariable.Value.Regex))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass command regex validation ({inputVariable.Value.Regex})"));
                    continue;
                }

                if (profileArgumentConstraints.TryGetValue(inputVariable.Key, out var profileArgumentConstraint) && !Regex.IsMatch(arg.Value, profileArgumentConstraint))
                {
                    failed.Add(new InvocationArgumentError(inputVariable.Key, $"Provided value does not pass profile constraint regex validation ({profileArgumentConstraint})"));
                    continue;
                }

                var invocationArgument = inputVariable.Value.Type switch
                {
                    ArgumentType.String => new InvocationArgument(arg.Name, ArgumentType.String, arg.Value, arg.Secret),
                    _ => throw new ArgumentOutOfRangeException(nameof(inputVariable.Value.Type))
                };
                accepted.Add(invocationArgument);
            }

            if (failed.Any())
                throw new InvalidInvocationArgumentsException(failed);
            
            var environmentVariables = formCollection.Where(e => e.Key == "environment").SelectMany(e => e.Value).Select(env => env.Trim()).ToArray();
            return (accepted.AsReadOnly(), environmentVariables);
        }

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AgentDeploy.Models;

namespace AgentDeploy.Services
{
    public static class ReplacementUtils
    {
        private static readonly Regex VariableRegex = new(@"\$\(([\S]+?)\)", RegexOptions.Compiled);
        
        public static string HideSecrets(string text, ScriptInvocationContext invocationContext)
        {
            var sb = new StringBuilder(text);
            foreach (var secret in invocationContext.Arguments.Where(arg => arg.Secret && !string.IsNullOrWhiteSpace(arg.Value)))
            {
                sb.Replace(secret.Value, new string('*', secret.Value.Length));
            }

            return sb.ToString();
        }
        
        public static string ReplaceVariables(string script, Dictionary<string, string> replacements)
        {
            return VariableRegex.Replace(script, match =>
            {
                var key = match.Groups[1].Value;
                if (replacements.TryGetValue(key, out var value))
                    return value;
                return $"$({key})";
            });
        }
        
        public static string ReplaceVariable(string script, string key, string value)
        {
            return script.Replace($"$({key})", value);
        }

        public static string[] ExtractUsedVariables(string script)
        {
            return VariableRegex.Matches(script).Select(m => m.Groups[1].Value).ToArray();
        }
    }
}
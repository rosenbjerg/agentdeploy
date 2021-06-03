using System;
using System.Text.RegularExpressions;

namespace AgentDeploy.Services.TypeValidation
{
    public sealed class RegexTypeValidator : ITypeValidator
    {
        private readonly Lazy<Regex> _regex;

        public RegexTypeValidator(string regexString)
        {
            _regex = new Lazy<Regex>(() => new Regex(regexString, RegexOptions.Compiled));
        }

        public bool IsValid(string content) => _regex.Value.IsMatch(content);
    }
}
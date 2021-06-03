using System;
using System.Linq;

namespace AgentDeploy.Services.TypeValidation
{
    public sealed class UriTypeValidator : ITypeValidator
    {
        private readonly UriHostNameType[] _uriHostNameTypes;

        public UriTypeValidator(params UriHostNameType[] uriHostNameTypes)
        {
            _uriHostNameTypes = uriHostNameTypes;
        }
        public bool IsValid(string content)
        {
            var uri = Uri.CheckHostName(content);
            return _uriHostNameTypes.Contains(uri);
        }
    }
}
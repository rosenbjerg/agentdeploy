using Microsoft.AspNetCore.Mvc;

namespace AgentDeploy.ExternalApi.Filters
{
    public class AuthorizedAttribute : TypeFilterAttribute
    {
        public AuthorizedAttribute() : base(typeof(AuthorizedFilter)) { }
    }
}
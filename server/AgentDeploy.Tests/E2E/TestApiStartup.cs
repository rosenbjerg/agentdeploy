using AgentDeploy.ExternalApi;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AgentDeploy.Tests.E2E
{
    public class TestApiStartup : ApiStartup
    {
        public TestApiStartup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void AddReaders(IServiceCollection services)
        {
            var mockScriptReader = new Mock<IScriptReader>();
            var mockTokenReader = new Mock<ITokenReader>();
            
            services.AddSingleton<Mock<IScriptReader>>(mockScriptReader);
            services.AddSingleton<Mock<ITokenReader>>(mockTokenReader);
            services.AddSingleton<IScriptReader>(mockScriptReader.Object);
            services.AddSingleton<ITokenReader>(mockTokenReader.Object);
        }
    }
}
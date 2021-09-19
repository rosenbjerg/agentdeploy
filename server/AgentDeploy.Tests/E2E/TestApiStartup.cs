using AgentDeploy.ExternalApi;
using AgentDeploy.Models.Options;
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
            
            services.AddSingleton<IFileService>(new FileService(new ExecutionOptions()));
            services.AddSingleton(mockScriptReader);
            services.AddSingleton(mockTokenReader);
            services.AddSingleton(mockScriptReader.Object);
            services.AddSingleton(mockTokenReader.Object);
        }
    }
}
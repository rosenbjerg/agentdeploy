using AgentDeploy.Models.Options;
using AgentDeploy.Services.ScriptExecutors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentDeploy.ExternalApi
{
    public static class DependencyExtensions
    {
        public static IServiceCollection AddAgentDeployOptions(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection
                .AddValidatedOptions<ExecutionOptions>(configuration)
                .AddValidatedOptions<DirectoryOptions>(configuration)
                .AddValidatedOptions<AgentOptions>(configuration);
        }
        public static IServiceCollection AddScriptExecutors(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddScoped<IScriptExecutorFactory, ScriptExecutorFactory>()
                .AddScoped<ILocalScriptExecutor, LocalScriptExecutor>()
                .AddScoped<IExplicitPrivateKeySecureShellExecutor, ExplicitPrivateKeySecureShellExecutor>()
                .AddScoped<IImplicitPrivateKeySecureShellExecutor, ImplicitPrivateKeySecureShellExecutor>()
                .AddScoped<ISshPassSecureShellExecutor, SshPassSecureShellExecutor>();
        }
    }
}
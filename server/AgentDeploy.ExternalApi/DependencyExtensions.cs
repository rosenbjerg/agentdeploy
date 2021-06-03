using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Locking;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Scripts;
using AgentDeploy.Services.TypeValidation;
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
        public static IServiceCollection AddScriptInvocationServices(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddSingleton<ITypeValidationService, TypeValidationService>()
                .AddScoped<IInvocationContextService, InvocationContextService>()
                .AddScoped<IScriptExecutionService, ScriptExecutionService>()
                .AddScoped<IScriptInvocationService, ScriptInvocationService>()
                .AddScoped<IScriptInvocationFileService, ScriptInvocationFileService>()
                .AddScoped<IScriptTransformer, ScriptTransformer>()
                .AddScoped<IScriptInvocationLockService, ScriptInvocationLockService>();
        }
        public static IServiceCollection AddOperationContextServices(this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddScoped<IOperationContextService, OperationContextService>()
                .AddScoped(provider => provider.GetRequiredService<IOperationContextService>().Create())
                .AddScoped<IOperationContext>(provider => provider.GetRequiredService<OperationContext>());
        }
    }
}
﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentDeploy.ExternalApi
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddValidatedOptions<TOptions>(this IServiceCollection serviceCollection, IConfiguration configuration)
            where TOptions : class, new()
        {
            serviceCollection.AddOptions<TOptions>()
                .Bind(configuration.GetSection(typeof(TOptions).Name))
                .ValidateDataAnnotations();
            serviceCollection.AddScoped(provider => provider.GetRequiredService<IOptionsSnapshot<TOptions>>().Value);
            return serviceCollection;
        }
    }
}
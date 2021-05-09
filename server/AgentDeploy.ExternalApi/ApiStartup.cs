using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentDeploy.ExternalApi.Middleware;
using AgentDeploy.ExternalApi.Websocket;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Locking;
using AgentDeploy.Services.Scripts;
using AgentDeploy.Services.Websocket;
using AgentDeploy.Yaml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentDeploy.ExternalApi
{
    public class ApiStartup
    {
        private readonly IConfiguration _configuration;

        public ApiStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options => options
                .AddPolicy("Default", cors => cors
                    .AllowAnyOrigin()
                    .WithHeaders("Authorization")
                    .WithMethods("POST")));
            
            services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.All);
            services.AddDistributedMemoryCache();

            services.AddAgentDeployOptions(_configuration);

            AddReaders(services);

            services.AddScoped<IInvocationContextService, InvocationContextService>();
            services.AddScoped<IScriptExecutionService, ScriptExecutionService>();
            services.AddScoped<IScriptTransformer, ScriptTransformer>();

            services.AddScriptExecutors();

            services.AddHttpContextAccessor();
            services.AddScoped<IOperationContextService, OperationContextService>();
            services.AddScoped(provider => provider.GetRequiredService<IOperationContextService>().Create());
            services.AddScoped<IOperationContext>(provider => provider.GetRequiredService<OperationContext>());
            
            services.AddScoped<IConnectionAccepter, WebsocketConnectionAccepter>();

            services.AddSingleton<IProcessExecutionService, ProcessExecutionService>();
            services.AddSingleton<IScriptInvocationParser, ScriptInvocationParser>();
            services.AddSingleton<IScriptInvocationLockService, ScriptInvocationLockService>();
            services.AddSingleton<IConnectionHub, ConnectionHub>();
            services.AddYamlParser();
            
            services
                .AddControllers()
                .AddApplicationPart(typeof(ApiStartup).Assembly)
                .AddJsonOptions(options => ConfigureJsonSerializer(options.JsonSerializerOptions));
        }
        
        protected virtual void AddReaders(IServiceCollection services)
        {
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IScriptReader, ScriptReader>();
            services.AddScoped<ITokenReader, TokenReader>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AgentOptions agentOptions)
        {
            if (agentOptions.TrustXForwardedHeaders) app.UseForwardedHeaders();
            if (agentOptions.AllowCors) app.UseCors("Default");

            app.UseMiddleware<LoggingEnrichingMiddleware>();
            app.UseMiddleware<AuthenticationMiddleware>();
            app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }

        private void ConfigureJsonSerializer(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
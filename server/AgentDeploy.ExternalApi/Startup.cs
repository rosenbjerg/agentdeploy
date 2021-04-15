using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentDeploy.ExternalApi.Middleware;
using AgentDeploy.ExternalApi.Websocket;
using AgentDeploy.Models;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Script;
using AgentDeploy.Services.ScriptExecutors;
using AgentDeploy.Services.Websocket;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentDeploy.ExternalApi
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
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

            services.AddValidatedOptions<ExecutionOptions>(_configuration);
            services.AddValidatedOptions<DirectoryOptions>(_configuration);
            services.AddValidatedOptions<AgentOptions>(_configuration);
            
            services.AddScoped<ScriptReader>();
            services.AddScoped<InvocationContextService>();
            services.AddScoped<TokenReader>();
            services.AddScoped<ScriptExecutionService>();
            services.AddScoped<ScriptTransformer>();
            
            services.AddScoped<LocalScriptExecutor>();
            services.AddScoped<ExplicitPrivateKeySecureShellExecutor>();
            services.AddScoped<ImplicitPrivateKeySecureShellExecutor>();
            services.AddScoped<SshPassSecureShellExecutor>();

            services.AddHttpContextAccessor();
            services.AddScoped<OperationContextService>();
            services.AddScoped(provider => provider.GetRequiredService<OperationContextService>().Create());
            services.AddScoped<IOperationContext>(provider => provider.GetRequiredService<OperationContext>());
            
            services.AddScoped<IConnectionAccepter, WebsocketConnectionAccepter>();

            services.AddSingleton<IScriptInvocationParser, ScriptInvocationParser>();
            services.AddSingleton<ConnectionHub>();
            services.AddSingleton(_ => new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build());
            
            services
                .AddControllers()
                .AddJsonOptions(options => ConfigureJsonSerializer(options.JsonSerializerOptions));
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
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentDeploy.ExternalApi.Middleware;
using AgentDeploy.ExternalApi.Websocket;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            
            services.AddScoped<CommandReader>();
            services.AddScoped<ExecutionContextService>();
            services.AddScoped<TokenReader>();
            services.AddScoped<ScriptExecutionService>();
            services.AddScoped<ScriptTransformer>();
            services.AddScoped<LocalScriptExecutor>();
            services.AddScoped<SecureShellExecutor>();
            services.AddScoped<OperationContext>();
            services.AddScoped<ConnectionAccepter, WebsocketConnectionAccepter>();
            services.AddScoped<IOperationContext>(provider => provider.GetRequiredService<OperationContext>());

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
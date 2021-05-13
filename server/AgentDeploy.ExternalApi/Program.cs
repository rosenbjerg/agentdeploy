using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AgentDeploy.ExternalApi
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder<ApiStartup>(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder<TStartup>(string[] args) where TStartup : class =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, builder) => builder.AddEnvironmentVariables("AGENTD_"))
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(o => o.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30))
                        .UseStartup<TStartup>();
                });
    }
}
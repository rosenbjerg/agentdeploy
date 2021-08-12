using System;
using System.Diagnostics;
using System.IO;
using AgentDeploy.ExternalApi;
using AgentDeploy.Models.Options;
using AgentDeploy.Services;
using AgentDeploy.Services.Scripts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

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

            services.AddSingleton<ILoggerFactory, TestLoggerFactory>();
            services.AddSingleton<IFileService>(new FileService(new ExecutionOptions()));
            services.AddSingleton(mockScriptReader);
            services.AddSingleton(mockTokenReader);
            services.AddSingleton(mockScriptReader.Object);
            services.AddSingleton(mockTokenReader.Object);
        }
    }

    public class TestLoggerFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName);
        }
    }
    public class TestLogger : ILogger
    {
        private readonly string _categoryName;

        public TestLogger(string categoryName)
        {
            _categoryName = categoryName;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return new MemoryStream();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception == null) return;
            Debug.WriteLine(exception.StackTrace);
            Trace.WriteLine(exception.StackTrace);
            TestContext.WriteLine(exception.StackTrace);
        }
    }
}
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

            var testLoggerFactory = new TestLoggerFactory();
            services.AddSingleton<ILoggerFactory>(testLoggerFactory);
            services.AddSingleton(testLoggerFactory);
            
            services.AddSingleton<IFileService>(new FileService(new ExecutionOptions()));
            services.AddSingleton(mockScriptReader);
            services.AddSingleton(mockTokenReader);
            services.AddSingleton(mockScriptReader.Object);
            services.AddSingleton(mockTokenReader.Object);
        }
    }

    public class TestLoggerFactory : ILoggerFactory
    {
        private string? _stacktrace;

        public void Dispose()
        {
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, this);
        }

        public string GetExceptionStacktrace()
        {
            var stacktrace = new string(_stacktrace);
            _stacktrace = null;
            return stacktrace;
        }

        public void SetExceptionStacktrace(string stacktrace)
        {
            _stacktrace = stacktrace;
        }
    }
    public class TestLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly TestLoggerFactory _testLoggerFactory;

        public TestLogger(string categoryName, TestLoggerFactory testLoggerFactory)
        {
            _categoryName = categoryName;
            _testLoggerFactory = testLoggerFactory;
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
            _testLoggerFactory.SetExceptionStacktrace($"{exception.Message}: {exception.StackTrace}");
            TestContext.Progress.WriteLine($"{exception.Message}: {exception.StackTrace}");
            TestContext.Error.WriteLine($"{exception.Message}: {exception.StackTrace}");
        }
    }
}
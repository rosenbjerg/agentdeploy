using System;
using AgentDeploy.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AgentDeploy.Services.ScriptExecutors
{
    public class ScriptExecutorFactory : IScriptExecutorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ScriptExecutorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        public IScriptExecutor Build(ScriptInvocationContext invocationContext)
        {
            if (invocationContext.SecureShellOptions == null)
                return _serviceProvider.GetRequiredService<ILocalScriptExecutor>();
            
            if (!string.IsNullOrEmpty(invocationContext.SecureShellOptions.Password))
                return _serviceProvider.GetRequiredService<ISshPassSecureShellExecutor>();
            
            if (!string.IsNullOrEmpty(invocationContext.SecureShellOptions.PrivateKeyPath))
                return _serviceProvider.GetRequiredService<IExplicitPrivateKeySecureShellExecutor>();
            
            return _serviceProvider.GetRequiredService<IImplicitPrivateKeySecureShellExecutor>();
        }
    }
}
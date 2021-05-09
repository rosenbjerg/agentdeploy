using System;

namespace AgentDeploy.Services.Locking
{
    public sealed class ScriptInvocationLock : IScriptInvocationLock
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public ScriptInvocationLock(Action onDispose)
        {
            _onDispose = onDispose;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _onDispose();
                _disposed = true;
            }
        }
    }
}
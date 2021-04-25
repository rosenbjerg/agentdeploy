using System;

namespace AgentDeploy.Services.Locking
{
    public class ScriptInvocationLock : IScriptInvocationLock
    {
        private readonly Action _onDispose;
        private bool _disposed = false;

        public ScriptInvocationLock(Action onDispose)
        {
            _onDispose = onDispose;
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing) 
                    _onDispose();
                _disposed = true;
            }
        }
        
        public void Dispose() // Implement IDisposable
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScriptInvocationLock() // the finalizer
        {
            Dispose(false);
        }
    }
}
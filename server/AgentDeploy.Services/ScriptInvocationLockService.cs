using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services.Locking;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services
{
    public sealed class ScriptInvocationLockService : IScriptInvocationLockService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<ScriptInvocationLockService> _logger;
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1,1);
        

        public ScriptInvocationLockService(IDistributedCache distributedCache, ILogger<ScriptInvocationLockService> logger)
        {
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<IScriptInvocationLock> Lock(Script script, string token,
            CancellationToken cancellationToken)
        {
            return script.Concurrency switch
            {
                ConcurrentExecutionLevel.Full => new ScriptInvocationLock(() => { }),
                ConcurrentExecutionLevel.None => await LockInternal(script.Name, string.Empty, cancellationToken),
                ConcurrentExecutionLevel.PerToken => await LockInternal(script.Name, token, cancellationToken),
                _ => throw new ArgumentOutOfRangeException(nameof(script.Concurrency), $"Unknown {nameof(ConcurrentExecutionLevel)}")
            };
        }

        private void Unlock(string key)
        {
            _distributedCache.Remove(key);
            _logger.LogDebug("Unlocking {Key}", key);
        }

        private async Task<IScriptInvocationLock> LockInternal(string scriptName, string token, CancellationToken cancellationToken)
        {
            await Semaphore.WaitAsync(cancellationToken);

            try
            {                
                var key = $"lock:{scriptName}:{token}".TrimEnd(':');
                _logger.LogDebug("Locking {Key}", key);
                var current = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(current))
                    throw new ScriptLockedException(scriptName);

                await _distributedCache.SetStringAsync(key, "locked", cancellationToken);
                return new ScriptInvocationLock(() => Unlock(key));
            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}
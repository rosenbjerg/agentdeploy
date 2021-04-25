using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using Microsoft.Extensions.Caching.Distributed;

namespace AgentDeploy.Services.Locking
{
    public class ScriptInvocationLockService : IScriptInvocationLockService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1,1);
        

        public ScriptInvocationLockService(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<IScriptInvocationLock> Lock(Script script, string token)
        {
            return script.Concurrency switch
            {
                ConcurrentExecutionLevel.Full => new ScriptInvocationLock(() => { }),
                ConcurrentExecutionLevel.None => await LockInternal(script.Name, string.Empty),
                ConcurrentExecutionLevel.PerToken => await LockInternal(script.Name, token),
                _ => throw new ArgumentOutOfRangeException(nameof(script.Concurrency), "Unknown ScriptLocking level")
            };
        }

        private async Task<IScriptInvocationLock> LockInternal(string scriptName, string token)
        {
            await _semaphore.WaitAsync();

            try
            {                
                var key = $"SL:{scriptName}:{token}".TrimEnd(':');
                var current = await _distributedCache.GetStringAsync(key);
                if (!string.IsNullOrEmpty(current))
                    throw new ScriptLockedException(scriptName);

                await _distributedCache.SetStringAsync(key, "locked");
                return new ScriptInvocationLock(() => _distributedCache.Remove(key));
            }
            finally
            {
                _semaphore.Release();
            }
            
        }
    }
}
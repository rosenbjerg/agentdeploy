using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Scripts;
using AgentDeploy.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace AgentDeploy.Tests.Unit
{
    [Category("Unit")]
    public class ScriptInvocationLockServiceTests
    {
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.Full, true)]
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.None, false)]
        [TestCase("test1", "test1", "tok1", "tok1", ConcurrentExecutionLevel.PerToken, false)]
        
        [TestCase("test1", "test1", "tok1", "tok2", ConcurrentExecutionLevel.None, false)]
        [TestCase("test1", "test1", "tok1", "tok2", ConcurrentExecutionLevel.PerToken, true)]

        [TestCase("test1", "test2", "tok1", "tok2", ConcurrentExecutionLevel.None, true)]
        [TestCase("test1", "test2", "tok1", "tok2", ConcurrentExecutionLevel.PerToken, true)]
        public async Task Locking(string scriptName1, string scriptName2, string token1, string token2, ConcurrentExecutionLevel concurrencyLevel, bool success)
        {
            var opts = Options.Create(new MemoryDistributedCacheOptions());
            var cache = new MemoryDistributedCache(opts);

            var service = new ScriptInvocationLockService(cache, NullLogger<ScriptInvocationLockService>.Instance);

            var script1 = new Script { Name = scriptName1, Concurrency = concurrencyLevel };
            var script2 = new Script { Name = scriptName2, Concurrency = concurrencyLevel };
            using var lock1 = await service.Lock(script1, token1, CancellationToken.None);
            
            Assert.NotNull(lock1);
            if (success)
            {
                using var lock2 = await service.Lock(script2, token2, CancellationToken.None);
                Assert.NotNull(lock2);
            }
            else
            {
                Assert.ThrowsAsync<ScriptLockedException>(() => service.Lock(script2, token2, CancellationToken.None));
            }
        }
    }
}
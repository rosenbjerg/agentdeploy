using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services
{
    public static class PerformanceLoggingUtilities
    {
        public static async Task<T> Time<T>(string message, ILogger logger, Func<Task<T>> func)
        {
            var started = DateTime.UtcNow;
            var result = await func();
            var elapsed = DateTime.UtcNow.Subtract(started).TotalMilliseconds;
            logger.LogDebug($"{message} elapsed: {elapsed}ms");
            return result;
        }
    }
}
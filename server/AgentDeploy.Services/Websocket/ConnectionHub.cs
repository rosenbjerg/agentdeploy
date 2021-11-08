using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AgentDeploy.Models.Websocket;
using Microsoft.Extensions.Logging;

namespace AgentDeploy.Services.Websocket
{
    public sealed class ConnectionHub : IConnectionHub
    {
        private readonly ILogger<ConnectionHub> _logger;
        private readonly ConcurrentDictionary<Guid, ConnectionContext> _connectionTable = new();

        public ConnectionHub(ILogger<ConnectionHub> logger)
        {
            _logger = logger;
        }
        
        public async Task<bool> JoinSession(Guid webSocketSessionId, Connection connection)
        {
            var session = await AwaitSession(webSocketSessionId, 0.5f);
            if (session == null)
                return false;
            
            session.SetConnection(connection);
            return true;
        }

        private async Task<ConnectionContext?> AwaitSession(Guid webSocketSessionId, float timeoutSeconds)
        {
            _logger.LogDebug("Awaiting WebSocket session {SessionId}", webSocketSessionId);
            var roundLengthMs = 100;
            var rounds = timeoutSeconds * 1000 / roundLengthMs;
            for (var i = 0; i < rounds; i++)
            {
                _logger.LogTrace("Waiting round {CurrentRound}/{MaxRounds} of length {LengthMs} for session {SessionId}", i, rounds - 1, roundLengthMs, webSocketSessionId);
                await Task.Delay(TimeSpan.FromMilliseconds(roundLengthMs));
                if (_connectionTable.TryGetValue(webSocketSessionId, out var session))
                    return session;
            }

            return null;
        }

        public ConnectionContext PrepareSession(Guid webSocketSessionId)
        {
            var connectionContext = new ConnectionContext();
            _connectionTable[webSocketSessionId] = connectionContext;

            void OnConnectionContextOnDisconnected(object? sender, EventArgs eventArgs)
            {
                ((ConnectionContext)sender!).Disconnected -= OnConnectionContextOnDisconnected;
                _connectionTable.TryRemove(webSocketSessionId, out _);
            }

            connectionContext.Disconnected += OnConnectionContextOnDisconnected;
            _logger.LogDebug("Prepared for WebSocket session {SessionId}", webSocketSessionId);
            return connectionContext;
        }
    }
}
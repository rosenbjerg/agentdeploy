using System;
using System.Threading;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Websocket;

namespace AgentDeploy.Services.Websocket
{
    public sealed class ConnectionContext
    {
        private readonly object _lock = new();

        private bool IsConnected()
        {
            bool connected;
            lock (_lock)
                connected = Connection != null;
            return connected;
        }

        private Connection? Connection { get; set; }

        public event EventHandler? Disconnected;

        public void SetConnection(Connection connection)
        {
            lock (_lock)
                Connection = connection;
            connection.Disconnected += OnDisconnected;
        }

        private void OnDisconnected(object? sender, EventArgs e)
        {
            lock (_lock)
                Connection = null;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> AwaitConnection(int timeoutSeconds, CancellationToken cancellationToken)
        {
            var stepSize = 100;
            var steps = (timeoutSeconds * 1000) / stepSize;
            for (var i = 0; i < steps; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(stepSize), cancellationToken);
                if (IsConnected()) return true;
            }

            return false;
        }

        public void SendScript(string scriptContent)
        {
            Connection?.SendMessage(new Message("script", scriptContent));
        }
        public void SendOutput(ProcessOutput processOutput)
        {
            Connection?.SendMessage(new Message("output", processOutput));
        }
    }
}
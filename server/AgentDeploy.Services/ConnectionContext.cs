using System;
using System.Threading.Tasks;
using AgentDeploy.Services.Models;

namespace AgentDeploy.Services
{
    public class ConnectionContext
    {
        private object _lock = new();

        public bool IsConnected()
        {
            var connected = false;
            lock (_lock)
                connected = Connection != null;
            return connected;
        }
        public Connection? Connection { get; private set; }

        public event EventHandler Disconnected;

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

        public async Task<bool> AwaitConnection(int timeoutSeconds)
        {
            var stepSize = 100;
            var steps = (timeoutSeconds * 1000) / stepSize;
            for (var i = 0; i < steps; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(stepSize));
                if (IsConnected()) return true;
            }

            return false;
        }

        public void SendOutput(ProcessOutput processOutput)
        {
            Connection?.SendMessage(new Message("output", processOutput));
        }
    }
}
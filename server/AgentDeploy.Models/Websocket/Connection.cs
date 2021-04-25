using System;
using System.Threading.Tasks;

namespace AgentDeploy.Models.Websocket
{
    public abstract class Connection
    {
        protected Connection(IOperationContext operationContext)
        {
            OperationContext = operationContext;
        }
        
        public IOperationContext OperationContext { get; }

        public abstract Task SendMessage(Message message);

        public abstract Task KeepConnectionOpen();
        
        public event EventHandler<Message>? MessageReceived;
        public event EventHandler? Disconnected;

        protected void OnMessageReceived(Message message) => MessageReceived?.Invoke(this, message);
        protected void OnDisconnected() => Disconnected?.Invoke(this, EventArgs.Empty);
    }
}
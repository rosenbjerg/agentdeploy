using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AgentDeploy.Models;
using AgentDeploy.Models.Exceptions;
using AgentDeploy.Models.Websocket;
using Microsoft.AspNetCore.Http;

namespace AgentDeploy.ExternalApi.Websocket
{
    public class WebsocketConnection : Connection
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new();
        
        private readonly HttpContext _httpContext;
        private readonly IOperationContext _operationContext;
        private WebSocket? _websocket;
        
        static WebsocketConnection()
        {
            JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }
        
        public WebsocketConnection(HttpContext httpContext, IOperationContext operationContext)
        {
            _httpContext = httpContext;
            _operationContext = operationContext;
        }

        public override async Task SendMessage(Message message)
        {
            if (_websocket == null) throw new WebsocketException("WebSocket has not been accepted yet");
            if (_websocket.State != WebSocketState.Open) throw new WebsocketException("WebSocket connection is not open");
            await _websocket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, JsonSerializerOptions), WebSocketMessageType.Text, true, _httpContext.RequestAborted);
        }

        public override async Task KeepConnectionOpen()
        {
            if (_websocket != null) throw new WebsocketException("WebSocket has already been accepted");

            try
            {
                _websocket = await _httpContext.WebSockets.AcceptWebSocketAsync();
                var buffer = ArrayPool<byte>.Shared.Rent(4096);
                while (!_operationContext.OperationCancelled.IsCancellationRequested && _websocket.State is WebSocketState.Connecting or WebSocketState.Open)
                {
                    if (await ReceiveMessages(buffer))
                        break;
                }
            }
            finally
            {
                OnDisconnected();
            }
        }

        private async Task<bool> ReceiveMessages(byte[] buffer)
        {
            WebSocketReceiveResult message;
            try
            {
                message = await _websocket!.ReceiveAsync(new ArraySegment<byte>(buffer), _httpContext.RequestAborted);
            }
            catch (OperationCanceledException) { return true; }
            catch (WebSocketException) { return true; }
            catch (IOException) { return true; }

            if (message.MessageType == WebSocketMessageType.Close)
                return true;

            if (message.EndOfMessage && message.MessageType == WebSocketMessageType.Text)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Message>(buffer);
                    if (parsed != null)
                        OnMessageReceived(parsed);
                }
                catch (JsonException) { return false; }
            }

            return false;
        }
    }
}
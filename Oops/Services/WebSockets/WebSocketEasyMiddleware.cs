using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using Oops.Components;
using System.Collections.Concurrent;

namespace Oops.Services.WebSockets
{
    public class WebSocketEasyMiddleware
    {
        private static int _clientNumber;
        public static int GetClientNumber()
        {
            return _clientNumber;
        }
        private readonly RequestDelegate _next;
        private readonly IMqttService _mqttServer;

        public WebSocketEasyMiddleware(RequestDelegate next)
        {
            _next = next;
            _mqttServer = IoC.Get<IMqttService>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;
            
            var socket = await context.WebSockets.AcceptWebSocketAsync();
  
            await Receive(socket, async (result, buffer) =>
            {              
            });
        }

        private ConcurrentDictionary<WebSocket, bool> allSockets = new ConcurrentDictionary<WebSocket, bool>();
        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            Interlocked.Increment(ref _clientNumber);            
            allSockets.TryAdd(socket, true);

            int provierNumber = _mqttServer.GetClientNumber();
            allSockets.Keys.ToList().ForEach(x => SendMessageAsync(x, $"{provierNumber},{_clientNumber}"));
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                    cancellationToken: CancellationToken.None);
            }
            Interlocked.Decrement(ref _clientNumber);
            bool dummy;
            allSockets.TryRemove(socket, out dummy);
            allSockets.Keys.ToList().ForEach(x => SendMessageAsync(x, $"{provierNumber},{_clientNumber}"));
        }

        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
                return;

            await socket.SendAsync(buffer: new ArraySegment<byte>(array: Encoding.ASCII.GetBytes(message),
                    offset: 0,
                    count: message.Length),
                messageType: WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
        }
    }
}

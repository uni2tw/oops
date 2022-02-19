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
        /// <summary>
        /// 取得目前在線上使用者數
        /// </summary>
        /// <returns></returns>
        public static int GetClientNumber()
        {
            return allSockets.Count;
        }

        private readonly RequestDelegate _next;
        private readonly IMqttService _mqttServer;
        private static System.Timers.Timer timer = new System.Timers.Timer(1000 * 10);

        public WebSocketEasyMiddleware(RequestDelegate next)
        {
            _next = next;
            _mqttServer = IoC.Get<IMqttService>();

            if (_mqttServer.OnChanged == null)
            {
                _mqttServer.OnChanged = delegate (int provierNumber)
                {
                    var theScokets = allSockets.Keys.ToList().Select(x => x.WebSocket).ToList();
                    theScokets.ForEach(x => SendMessageAsync(x, $"{provierNumber},{theScokets.Count}"));

                };
            }

            timer.Elapsed += async delegate (object sender, System.Timers.ElapsedEventArgs e)
            {
                int? newCount = null;
                DateTime now = DateTime.Now;

                foreach (var clientInfo in allSockets.Keys.ToList())
                {
                    if ((now - clientInfo.LastTouchTime).TotalSeconds > 20)
                    {
                        if (clientInfo.WebSocket.State == WebSocketState.Open)
                        {
                            clientInfo.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "timeout", CancellationToken.None);
                        }
                    }
                }
            };
            timer.Start();
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

        private static System.Collections.Concurrent.ConcurrentDictionary<SocketInfo, bool> allSockets =
            new ConcurrentDictionary<SocketInfo, bool>();

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            int provierNumber = _mqttServer.GetClientNumber();
            int clientNumber = 0;
            Guid socketId = Guid.NewGuid();
            SocketInfo socketInfo = new SocketInfo { Id = socketId, WebSocket = socket, LastTouchTime = DateTime.Now };

            allSockets.TryAdd(socketInfo, true);
            clientNumber = allSockets.Count;

            allSockets.Keys.ToList().ForEach(x => SendMessageAsync(x.WebSocket, $"{provierNumber},{clientNumber},{socketId}"));

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                    cancellationToken: CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string text = System.Text.Encoding.UTF8.GetString(buffer).Substring(0, result.Count);
                    string[] args = text.Split(' ');
                    if (args[0] == "health_check")
                    {
                        string id = args[1];

                        var socketForCheck = allSockets.Keys.FirstOrDefault(x => x.Id.ToString() == id);
                        if (socketForCheck != null)
                        {
                            socketForCheck.LastTouchTime = DateTime.Now;
                        }
                    }
                }
            }

            //刪除後重送資料

            bool dummy;
            allSockets.TryRemove(socketInfo, out dummy);

            provierNumber = _mqttServer.GetClientNumber();
            allSockets.Keys.ToList().ForEach(x => SendMessageAsync(x.WebSocket, $"{provierNumber},{allSockets.Count}"));
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

    public class SocketInfo
    {
        public Guid Id { get; set; }
        public DateTime LastTouchTime { get; set; }
        public WebSocket WebSocket { get; set; }
    }
}

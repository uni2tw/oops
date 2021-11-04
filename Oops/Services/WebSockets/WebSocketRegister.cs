using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Oops.Services.WebSockets
{
    public static class WebSocketRegister
    {
        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketEasyMiddleware>());
        }
    }
}

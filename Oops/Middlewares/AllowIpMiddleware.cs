using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Oops
{
    public class AllowIpMiddleware
    {
        private readonly RequestDelegate _next;      

        public AllowIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            string ip = GetClientIp(context);
            string forwardIp = GetForwardedClientIp(context);
            if (forwardIp == "59.120.143.229" || ip == "59.120.143.229" || ip == "0.0.0.1")
            {
                await _next(context);
            }
            else
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = 400;
                string msg = string.Format("{0} {1} wasn't in the white ip list.", ip, forwardIp);
                await context.Response.WriteAsync(msg);
            }            
        }

        private string GetForwardedClientIp(HttpContext context)
        {
            string clientIp;
            StringValues strValues;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out strValues))
            {
                clientIp = strValues.FirstOrDefault() ?? string.Empty;
                if (clientIp.Contains(','))
                {
                    clientIp = clientIp.Split(',')[0].Trim();
                }
                return clientIp;
            }
            return string.Empty;
        }

        private string GetClientIp(HttpContext context)
        {
            string clientIp = context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            return clientIp;
        }
    }

}
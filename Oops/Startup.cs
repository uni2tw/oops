using System;
using System.IO;
using Oops.Components;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Oops.Services.WebSockets;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Oops.Daos;

namespace Oops
{
    public class Startup
    {
        IConfiguration _configuration;
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {            
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
                });
        }
        public void Configure(IApplicationBuilder app)
        {            
            string assetsPath = Helper.MapPath("assets");
            if (Directory.Exists(assetsPath) == false)
            {
                string msg = string.Format("內容目錄 {0} 不存在，或未設定正確", assetsPath);
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assetsPath),
                RequestPath = new PathString("/oops"),
                ServeUnknownFileTypes = true
            });

            if (_configuration.GetSection("AllowIpMiddleware").GetValue<bool?>("Enabled").GetValueOrDefault())
            {
                app.UseMiddleware<AllowIpMiddleware>();
            }            
            app.UseRouting();
            app.UseEndpoints(config =>
            {                
                config.MapControllers();
            });
            app.UseWebSockets();
            app.MapWebSocketManager("/ws");
        }
    }
}

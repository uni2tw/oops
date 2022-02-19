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
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Oops.Services;

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
            TimeSpan offsetTime = TimeSpan.Zero;
            string offsetTimeValue = _configuration.GetSection("OffsetTime").Value;
            if (string.IsNullOrEmpty(offsetTimeValue) == false)
            {
                TimeSpan.TryParse(offsetTimeValue, out offsetTime);
            }
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
                    options.JsonSerializerOptions.Converters.Add(new LocalDateTimeConverter(offsetTime));
                });
        }
        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
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
            app.MapWebSocketManager("/oops/ws");

            appLifetime.ApplicationStopping.Register(() =>
            {
                IoC.Get<IMqttService>().Stop();
                IoC.Get<LogDao>().Flush();
            });
        }
    }

    public class LocalDateTimeConverter : JsonConverter<DateTime>
    {
        TimeSpan _offsetTime;
        public LocalDateTimeConverter(TimeSpan offsetTime)
        {
            _offsetTime = offsetTime;
        }
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Add(_offsetTime).ToString("yyyy-MM-ddTHH:mm:ss"));
        }
    }
}

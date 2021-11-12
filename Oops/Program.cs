using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using oops.DataModels;
using Oops.Components;
using Oops.Daos;
using Oops.DataModels;
using Oops.Services;
using Oops.Services.WebSockets;
using static System.Net.Mime.MediaTypeNames;

namespace Oops
{
    class Program
    {
        static void Main(string[] args)
        {         
            Console.WriteLine("Root " + Helper.MapPath(string.Empty));

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
            });

            var env = builder.Environment;
            IoC.Register();
            var confRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, true)
                .AddEnvironmentVariables()
                .Build();
            builder.Configuration.AddConfiguration(confRoot);
            var app = builder.Build();

            IoC.Get<IDBConnectionConfig>().ConnectionString = confRoot.GetSection("DBSetting:ConnectionString").Value;

            ErrorDao dao = IoC.Get<ErrorDao>();
            Console.WriteLine("LoadConnectString: " + dao.LoadConnectString());
            DbUtil.EnsureDBFile(dao.LoadConnectString());
            DbUtil.EnsureTable<Error>(dao.LoadConnectString());
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Time");
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Application", "Time");

            DbUtil.EnsureTable<OopsLog>(dao.LoadConnectString());
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Date), nameof(OopsLog.Srv), nameof(OopsLog.Logger));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Date), nameof(OopsLog.Logger), nameof(OopsLog.Srv));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Logger), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Logger), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Srv), nameof(OopsLog.Date));

            DbUtil.EnsureTable<OopsApi>(dao.LoadConnectString());
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Date), nameof(OopsApi.Url));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Date), nameof(OopsApi.Srv));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Srv), nameof(OopsApi.Url));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Url));
            IoC.Get<IMqttService>().Start();

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

            if (builder.Configuration.GetSection("AllowIpMiddleware").GetValue<bool?>("Enabled").GetValueOrDefault())
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
            app.Run();

            IoC.Get<IMqttService>().Stop();

        }
    }
}
 
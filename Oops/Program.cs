using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Oops.Components;
using Oops.Daos;
using Oops.Services;
using Oops.Services.WebSockets;

namespace Oops;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Root " + Helper.MapPath(string.Empty));


        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(cfg =>
            {
                cfg.UseKestrel((builderContext, options) =>
                {
                });
                cfg.UseStartup<Startup>();
            })
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                var env = ctx.HostingEnvironment;
                IoC.Register();
                var confRoot = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, true)
                    .AddEnvironmentVariables()
                    .Build();

                cfg.AddConfiguration(confRoot);
                IoC.Get<IDBConnectionConfig>().ConnectionString = confRoot.GetSection("DBSetting:ConnectionString").Value;

                ErrorDao dao = IoC.Get<ErrorDao>();
                Console.WriteLine("LoadConnectString: " + dao.LoadConnectString());


                int? port = confRoot.GetValue<int?>("Port");
                IoC.Get<IMqttService>().Start(port);
            })
            .Build();

        host.Run();
    }
}

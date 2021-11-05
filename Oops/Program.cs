using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Oops.Components;
using Oops.Daos;
using Oops.DataModels;
using Oops.Services;

namespace Oops
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                Helper.SetRoot(args[0].Trim('\'', '"'));
            }

            Console.WriteLine(Helper.MapPath("assets"));

            IoC.Register();

            IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                .AddJsonFile(Helper.MapPath("appsettings.json"), false)
                .AddEnvironmentVariables()
                .Build();            
            IoC.Get<IDBConnectionConfig>().ConnectionString = configurationRoot.GetSection("DBSetting:ConnectionString").Value;

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

            IoC.Get<IMqttService>().Start();

            var host = Host.CreateDefaultBuilder()                
                .ConfigureWebHost(cfg =>
                {
                    cfg.UseKestrel(option =>
                    {
                        option.ListenAnyIP(1882);
                    });
                    cfg.UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;
                    cfg.AddConfiguration(configurationRoot);
                })
                .Build();

            host.Run();

            IoC.Get<IMqttService>().Stop();

        }
    }
}

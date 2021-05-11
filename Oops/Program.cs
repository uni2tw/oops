using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            Console.WriteLine(Helper.MapPath("assets"));

            IoC.Register();

            ErrorDao dao = IoC.Get<ErrorDao>();
            Console.WriteLine("LoadConnectString: " + dao.LoadConnectString());
            DbUtil.EnsureTable<Error>(dao.LoadConnectString());
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Time");
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Application", "Time");            
                
            IoC.Get<IMqttService>().Start();

            var host = Host.CreateDefaultBuilder()                
                .ConfigureWebHost(cfg =>
                {
                    cfg.UseKestrel(option =>
                    {
                        option.ListenAnyIP(1882);
                    });
                    cfg.UseStartup<Startup>();
                }).Build();

            host.Run();

            IoC.Get<IMqttService>().Stop();

        }
    }
}

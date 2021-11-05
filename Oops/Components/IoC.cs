using System;
using Microsoft.Extensions.Caching.Memory;
using Oops.Daos;
using Oops.Services;
using Autofac;
using Autofac.Core;

namespace Oops.Components
{
    public class IoC
    {
        private static IContainer container;
        public static void Register()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<MqttService>().As<IMqttService>().SingleInstance();
            builder.RegisterInstance(new ErrorDao());
            builder.RegisterInstance(new LogDao());
            builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions() { }));
            builder.RegisterInstance<IDBConnectionConfig>(new DBConnectionConfig());
            container = builder.Build();

        }

        public static T Get<T>() where T : class
        {
            // if (typeof(T).IsInterface == false)
            // {
            //     throw new Exception("T must be interface");
            // }
            return container.Resolve<T>();
        }

        public static T Get<T>(string name) where T : class
        {
            // if (typeof(T).IsInterface == false)
            // {
            //     throw new Exception("T must be interface");
            // }
            return container.ResolveNamed<T>(name);
        }

        public static MemoryCache GetCache()
        {
            return container.Resolve<MemoryCache>();
        }
    }
}
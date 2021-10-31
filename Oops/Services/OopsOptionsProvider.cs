using Autofac.Core;
using Oops.DataModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Oops.Services
{

    public class OopsOptionsProvider
    {
        private ObjectCache _cache = System.Runtime.Caching.MemoryCache.Default;
        private OopsOptionsProvider()
        {

        }
        private static OopsOptionsProvider _instance = new OopsOptionsProvider();
        public static OopsOptionsProvider Current
        {
            get
            {
                return _instance;
            }
        }
        System.Timers.Timer _timer;
        private static object _thisLock = new object();
        public void Start()
        {
            this.Loggers = new List<string>();
            this.RecentlyLoggers = new List<string>();
            this.LoggersSet = new HashSet<string>();

            this.Services = new List<string>();
            this.RecentlyServices = new List<string>();
            this.ServiceSet = new HashSet<string>();

            this.Dates = new List<string>();
            this.RecentlyDates = new List<string>();
            this.DateSet = new HashSet<string>();

            this.DateHours = new List<string>();
            this.RecentlyDateHours = new List<string>();
            this.DateHourSet = new HashSet<string>();

            var version = (int)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            this.Services.Add("ord");
            this.Services.Add("prd");
            this.Services.Add("pay");
            this.ServicesVersion = version;
            this.Dates.Add("20211030");
            this.Dates.Add("20211031");
            this.DatesVersion = version;
            this.DateHours.Add("2021103007");
            this.DateHours.Add("2021103008");
            this.DateHours.Add("2021103009");
            this.DateHours.Add("2021103010");
            this.DateHoursVersion = version;
            this.Loggers.Add("PXEC.OMO.GroupBuying.Order.API.Middlewares.APILogMiddleware");
            this.Loggers.Add("PXEC.OMO.GroupBuying.Order.API.Application.Behaviors.LoggingBehavior");
            this.Loggers.Add("PXEC.OMO.GroupBuying.Order.API.Application.Commands.RefundPaymentCommandHandler");
            this.LoggersVersion = version;


            if (_timer == null)
            {                
                _timer = new System.Timers.Timer();
                _timer.Interval = TimeSpan.FromMinutes(10).TotalMilliseconds;
                _timer.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e)
                {
                    try
                    {
                        this._running = true;










                    }
                    finally
                    {
                        this._running = false;
                    }
                };
                _timer.Start();
            }
        }

        
        bool _running = false;
        public void Stop()
        {
            _timer.Stop();
            while (_running = false)
            {
                Thread.Sleep(5000);
            }            
        }

        public void Parse(OopsLog log)
        {
            lock (_thisLock)
            {
                if (LoggersSet.Contains(log.Logger) == false)
                {
                    LoggersSet.Add(log.Logger);
                    RecentlyLoggers.Add(log.Logger);
                    RecentlyLoggers = RecentlyLoggers.OrderBy(x => x).ToList();
                }

                if (ServiceSet.Contains(log.Srv) == false)
                {
                    ServiceSet.Add(log.Srv);
                    RecentlyServices.Add(log.Srv);
                    RecentlyServices = RecentlyServices.OrderBy(x => x).ToList();
                }

                if (DateSet.Contains(log.Date) == false)
                {
                    DateSet.Add(log.Date);
                    RecentlyDates.Add(log.Date);
                    RecentlyDates = RecentlyDates.OrderBy(x => x).ToList();
                }

                if (DateHourSet.Contains(log.DateHour) == false)
                {
                    DateHourSet.Add(log.DateHour);
                    RecentlyDateHours.Add(log.DateHour);
                    RecentlyDateHours = RecentlyDateHours.OrderBy(x => x).ToList();
                }
            }
        }

        public OopsLogMultipleOptions GetMultipleOptions(int expiryMinutes)
        {
            string key = $"{nameof(OopsLogMultipleOptions)}://{expiryMinutes}";
            OopsLogMultipleOptions result = _cache.Get(key) as OopsLogMultipleOptions;
            if (result != null)
            {
                return result;
            }
            lock (_thisLock)
            {
                var loggers = new List<string>();
                loggers.AddRange(this.Loggers);
                loggers.AddRange(this.RecentlyLoggers);
                loggers.Sort();

                var services = new List<string>();
                services.AddRange(this.Services);
                services.AddRange(this.RecentlyServices);
                services.Sort();

                var dates = new List<string>();
                dates.AddRange(this.Dates);
                dates.AddRange(this.RecentlyDates);              
                dates.Sort();

                var dateHours = new List<string>();
                dateHours.AddRange(this.DateHours);
                dateHours.AddRange(this.RecentlyDateHours);
                dateHours.Sort();

                result = new OopsLogMultipleOptions
                {
                    Logger = new OopsLogOptions
                    {
                        Options = loggers,
                        Version = this.LoggersVersion
                    },
                    Service = new OopsLogOptions
                    {
                        Options = services,
                        Version = this.ServicesVersion
                    },
                    DateHour = new OopsLogOptions
                    {
                        Options = dateHours,
                        Version = this.DateHoursVersion
                    },
                    Date = new OopsLogOptions
                    {
                        Options = dates,
                        Version = this.DatesVersion
                    }
                };
                _cache.Set(key, result, DateTime.Now.AddMinutes(expiryMinutes));
                return result;
            }
        }



        public int LoggerLastId { get; set; }
        public List<string> Loggers { get; set; }
        public List<string> RecentlyLoggers { get; set; }
        public HashSet<string> LoggersSet { get; set; }

        public int ServiceLastId { get; set; }
        public List<string> Services { get; set; }
        public List<string> RecentlyServices { get; set; }
        public HashSet<string> ServiceSet { get; set; }

        public int DateLastId { get; set; }
        public List<string> Dates { get; set; }
        public List<string> RecentlyDates { get; set; }
        public HashSet<string> DateSet { get; set; }

        public int DateHourLastId { get; set; }
        public List<string> DateHours { get; set; }
        public List<string> RecentlyDateHours { get; set; }
        public HashSet<string> DateHourSet { get; set; }
        public int ServicesVersion { get; private set; }
        public int DatesVersion { get; private set; }
        public int DateHoursVersion { get; private set; }
        public int LoggersVersion { get; private set; }
    }

    public class OopsLogMultipleOptions
    {
        public OopsLogOptions Logger { get; set; }
        public OopsLogOptions Service { get; set; }
        public OopsLogOptions Date { get; set; }
        public OopsLogOptions DateHour { get; set; }
    }

    public class OopsLogOptions
    {
        public List<string> Options { get; set; }
        public int Version { get; set; }
    }
}

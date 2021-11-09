using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Oops.DataModels;
using System.Diagnostics;

namespace Oops.Daos
{

    public class LogDao : DaoBase
    {
        System.Collections.Concurrent.ConcurrentQueue<OopsLog> pendingLogs = new System.Collections.Concurrent.ConcurrentQueue<OopsLog>();
        public int GetPendingLogsCount()
        {
            return pendingLogs.Count;
        }
        const int VeryBusyInterval = 100;
        const int BusyInterval = 1000;
        const int NormalBusyInterval = 5000;
        public string GetStatus()
        {
            if (timer.Interval == VeryBusyInterval)
            {
                return "very_busy";
            }
            else if (timer.Interval == BusyInterval)
            {
                return "busy";
            }
            return "normal";
        }
        public System.Timers.Timer timer = new System.Timers.Timer();
        public LogDao()
        {
            timer.Interval = 5000;
            timer.AutoReset = false;
            timer.Start();            
            timer.Elapsed += Timer_Elapsed;
        }
        
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                List<OopsLog> logs = new List<OopsLog>();
                while (pendingLogs.Count > 0 && logs.Count < 1000)
                {
                    OopsLog temp;
                    if (pendingLogs.TryDequeue(out temp))
                    {
                        logs.Add(temp);
                    }
                }
                if (logs.Count > 0)
                {
                    int effectRows = BulkInert(logs);
                }
            }
            finally
            {
                if (pendingLogs.Count > 100000)
                {
                    timer.Interval = 100;
                }
                else if (pendingLogs.Count > 10000)
                {
                    timer.Interval = 1000;
                }
                else
                {
                    timer.Interval = 5000;
                }
                timer.Start();

            }
        }

        private int BulkInert(List<OopsLog> logs)
        {
            return BulkInert2(logs);
            Stopwatch watch = new Stopwatch();
            watch.Start();            
            int count = 0;
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                foreach (var log in logs)
                {
                    if (conn.Insert(log) > 0) { count++; }
                }
            }
            var elapsedSecs = watch.Elapsed.TotalSeconds.ToString("0.00");
            Console.WriteLine($"put {count} logs: {elapsedSecs} secs.");
            
            return count;
        }

        private int BulkInert2(List<OopsLog> logs)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (SQLiteConnection conn = new SQLiteConnection(LoadConnectString()))
            {                
                conn.Open();
                conn.BulkInsert(logs);
            }
            var elapsedSecs = watch.Elapsed.TotalSeconds.ToString("0.00");
            Console.WriteLine($"put {logs.Count} logs: {elapsedSecs} secs.");

            return logs.Count;
        }



        public void InsertLog(OopsLog log)
        {
            pendingLogs.Enqueue(log);
        }
        public async Task<bool> InsertLogAsaync(OopsLog log)
        {            
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                var count = await conn.InsertAsync(log);

                return count > 0;      
            }
        }

        public async Task<LogsResponse> GetLogs(string service, string logger,
            OopsLogLevel minLevel, OopsLogLevel maxLevel,
            string date, int page, int pageSize)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlCountHead = "select count(*) from log";
                string sqlQueryHead = "select * from log";

                string sqlWhere = " where 1=1";
                var parameters = new DynamicParameters();
                if (string.IsNullOrEmpty(service) == false)
                {
                    sqlWhere += " and srv=@service";
                    parameters.Add("@service", service);
                }
                if (string.IsNullOrEmpty(logger) == false)
                {
                    sqlWhere += " and logger=@logger";
                    parameters.Add("@logger", logger);
                }
                if (minLevel != OopsLogLevel.Trace)
                {
                    sqlWhere += " and level>=@level";
                    parameters.Add("@level", minLevel);
                }
                if (maxLevel != OopsLogLevel.Off)
                {
                    sqlWhere += " and level<=@level2";
                    parameters.Add("@level2", maxLevel);
                }
                if (string.IsNullOrEmpty(date) == false)
                {
                    sqlWhere += " and date=@date";
                    parameters.Add("@date", date);
                }
                //if (string.IsNullOrEmpty(dateWithHour) == false)
                //{
                //    sqlWhere = " and dateWithHour=@dateWithHour";
                //    parameters.Add("@dateWithHour", dateWithHour);
                //}

                string sqlCount = string.Format("{0} {1}", sqlCountHead, sqlWhere);
                LogsResponse response = new LogsResponse();

                DateTime now = DateTime.Now;

                response.TotalRows = await conn.ExecuteScalarAsync<int>(sqlCount, parameters);
                response.TotalPage = (response.TotalRows / pageSize) + (response.TotalRows % pageSize == 0 ? 0 : 1);
                if (page > response.TotalPage)
                {
                    response.CurrentPage = 1;
                }
                else
                {
                    response.CurrentPage = page;
                }

                int startIndex = (page - 1) * pageSize;
                string sqlQuery = string.Format("{0} {1} order by id desc limit {2},{3}",
                    sqlQueryHead, sqlWhere, startIndex, pageSize);

                List<OopsLog> logs = conn.Query<OopsLog>(sqlQuery, parameters).ToList();
                response.Logs = logs;

                var elapsedSecs = (DateTime.Now - now).TotalSeconds.ToString("0.00");
                Console.WriteLine($"get logs: {elapsedSecs} secs.");
                return response;
            }
        }

        public async Task<GetOptionsResponse> GetOptionsAsync()
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                DateTime now = DateTime.Now;
                var getServicesTask = conn.QueryAsync<string>("select distinct srv from log order by srv");
                var getLoggersTask = conn.QueryAsync<string>("select distinct logger from log order by logger");
                var getDatesTask = conn.QueryAsync<string>("select distinct date from log order by date");
                await Task.WhenAll(getServicesTask, getLoggersTask, getDatesTask);
                GetOptionsResponse response = new GetOptionsResponse
                {
                    Services = getServicesTask.Result.ToList(),
                    Loggers = getLoggersTask.Result.ToList(),
                    Dates = getDatesTask.Result.ToList(),
                };
                var elapsedSecs = (DateTime.Now - now).TotalSeconds.ToString("0.00");
                Console.WriteLine($"get options: {elapsedSecs} secs.");
                return response;
            }
        }

        public class GetOptionsResponse
        {
            public List<string> Services { get; set; }
            public List<string> Loggers { get; set; }
            public List<string> Dates { get; set; }
        }
    }
}

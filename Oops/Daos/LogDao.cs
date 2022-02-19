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
using System.Threading;
using oops.Daos;

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
        const int NormalInterval = 5000;
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
                    timer.Interval = VeryBusyInterval;
                }
                else if (pendingLogs.Count > 10000)
                {
                    timer.Interval = BusyInterval;
                }
                else
                {
                    timer.Interval = NormalInterval;
                }
                timer.Start();
            }
        }

        private int BulkInert(List<OopsLog> logs)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (SQLiteConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                conn.BulkInsert(logs);
            }
            var elapsedSecs = watch.Elapsed.TotalSeconds.ToString("0.00");
            Console.Write($"{Environment.NewLine}put {logs.Count} logs: {elapsedSecs} secs.");

            return logs.Count;
        }



        public void InsertLog(OopsLog log)
        {
            pendingLogs.Enqueue(log);
            InsertOption(1, log.Srv);
            InsertOption(2, log.Logger);
            InsertOption(3, log.Date);
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
            string date, int page, int pageSize, string traceId)
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
                if (string.IsNullOrEmpty(traceId) == false)
                {
                    sqlWhere += " and traceId=@traceId";
                    parameters.Add("@traceId", traceId);
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
                Console.Write($"{Environment.NewLine}get logs: {elapsedSecs} secs.");
                return response;
            }
        }

        public async Task<GetOptionsResponse> GetOptions0Async()
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
                Console.Write($"{Environment.NewLine}get options: {elapsedSecs} secs.");
                return response;
            }
        }

        public async Task<bool> UpgradeForFastOption()
        {
            if ((await GetLogOptionCountAsaync()) == 0)
            {
                var response = await GetOptions0Async();
                foreach (var name in response.Services)
                {
                    OopsLogOption opt = new OopsLogOption
                    {
                        Name = name,
                        Type = 1
                    };
                    await InsertOptionAsaync(opt);
                }
                foreach (var name in response.Loggers)
                {
                    OopsLogOption opt = new OopsLogOption
                    {
                        Name = name,
                        Type = 2
                    };
                    await InsertOptionAsaync(opt);
                }
                foreach (var name in response.Dates)
                {
                    OopsLogOption opt = new OopsLogOption
                    {
                        Name = name,
                        Type = 3
                    };
                    await InsertOptionAsaync(opt);
                }
            }
            return true;
        }

        object _thisLock = new object();
        GetOptionsResponse _response;

        public async Task<GetOptionsResponse> GetOptionsAsync()
        {
            if (_response != null)
            {
                return _response;
            }
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                DateTime now = DateTime.Now;
                var options = await conn.QueryAsync<OopsLogOption>("select name as Name, type as Type from log_option");
                _response = new GetOptionsResponse
                {
                    Services = options.Where(x => x.Type == 1).OrderByDescending(x => x.Name).Select(x => x.Name).ToList(),
                    Loggers = options.Where(x => x.Type == 2).OrderByDescending(x => x.Name).Select(x => x.Name).ToList(),
                    Dates = options.Where(x => x.Type == 3).OrderByDescending(x => x.Name).Select(x => x.Name).ToList()
                };
                var elapsedSecs = (DateTime.Now - now).TotalSeconds.ToString("0.00");
                Console.Write($"{Environment.NewLine}get options: {elapsedSecs} secs.");
                return _response;
            }
        }

        public void InsertOption(int type, string name)
        {
            lock (_thisLock)
            {
                if (type == 1)
                {
                    if (_response.Services.FirstOrDefault(x => x == name) == null)
                    {
                        _response.Services.Add(name);
                        InsertOptionAsaync(new OopsLogOption
                        {
                            Name = name,
                            Type = 1
                        });
                    }
                }
                else if (type == 2)
                {
                    if (_response.Loggers.FirstOrDefault(x => x == name) == null)
                    {
                        _response.Loggers.Add(name);
                        InsertOptionAsaync(new OopsLogOption
                        {
                            Name = name,
                            Type = 2
                        });
                    }
                }
                else if (type == 3)
                {
                    if (_response.Dates.FirstOrDefault(x => x == name) == null)
                    {
                        _response.Dates.Add(name);
                        InsertOptionAsaync(new OopsLogOption
                        {
                            Name = name,
                            Type = 3
                        });
                    }
                }
            }
        }

        public async Task<bool> InsertOptionAsaync(OopsLogOption log)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                var count = await conn.InsertAsync(log);

                return count > 0;
            }
        }

        public async Task<int> GetLogOptionCountAsaync()
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlCount = "select count(*) from log_option";
                int count = await conn.ExecuteScalarAsync<int>(sqlCount);
                return count;
            }
        }

        public void Flush()
        {
            DateTime stoppingTime = DateTime.Now;
            timer.Stop();
            if (pendingLogs.Count == 0)
            {
                Console.WriteLine("程式已終止");
                return;
            }
            Console.WriteLine($"程式關閉中，處理 {pendingLogs.Count} 筆資料");
            while (pendingLogs.Count > 0)
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
                BulkInert(logs);
                //就等10秒
                if ((DateTime.Now - stoppingTime).TotalSeconds > 10)
                {
                    if (pendingLogs.Count > 0)
                    {
                        Console.WriteLine($"強制關閉，損失 {pendingLogs.Count} 筆資料");
                    }
                    break;
                }
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


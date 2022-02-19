using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;
using oops.DataModels;
using oops.Daos;

namespace Oops.Daos
{

    public class ApiDao : DaoBase
    {
        ConcurrentQueue<OopsApi> pendingApis = new ConcurrentQueue<OopsApi>();
        public int GetPendingAPIsCount()
        {
            return pendingApis.Count;
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
        public ApiDao()
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
                List<OopsApi> apis = new List<OopsApi>();
                while (pendingApis.Count > 0 && apis.Count < 1000)
                {
                    OopsApi temp;
                    if (pendingApis.TryDequeue(out temp))
                    {
                        apis.Add(temp);
                    }
                }
                if (apis.Count > 0)
                {
                    int effectRows = BulkInert(apis);
                }
            }
            finally
            {
                if (pendingApis.Count > 100000)
                {
                    timer.Interval = 100;
                }
                else if (pendingApis.Count > 10000)
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

        private int BulkInert(List<OopsApi> apis)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (SQLiteConnection conn = new SQLiteConnection(LoadConnectString()))
            {                
                conn.Open();
                conn.BulkInsert(apis);
            }
            var elapsedSecs = watch.Elapsed.TotalSeconds.ToString("0.00");
            Console.Write($"{Environment.NewLine}put {apis.Count} apis: {elapsedSecs} secs.");

            return apis.Count;
        }



        public void InsertApi(OopsApi api)
        {
            pendingApis.Enqueue(api);
        }
        public async Task<bool> InsertLogAsync(OopsApi api)
        {            
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                var count = await conn.InsertAsync(api);

                return count > 0;      
            }
        }

        public async Task<APIsResponse> GetAPIs(string service, 
            string date, int page, int pageSize)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlCountHead = "select count(*) from api";
                string sqlQueryHead = "select * from api";

                string sqlWhere = " where 1=1";
                var parameters = new DynamicParameters();
                if (string.IsNullOrEmpty(service) == false)
                {
                    sqlWhere += " and srv=@service";
                    parameters.Add("@service", service);
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
                APIsResponse response = new APIsResponse();

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

                List<OopsApi> apis = conn.Query<OopsApi>(sqlQuery, parameters).ToList();
                response.Apis = apis;

                var elapsedSecs = (DateTime.Now - now).TotalSeconds.ToString("0.00");
                Console.Write($"{Environment.NewLine}get logs: {elapsedSecs} secs.");
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
                Console.Write($"{Environment.NewLine}get options: {elapsedSecs} secs.");
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

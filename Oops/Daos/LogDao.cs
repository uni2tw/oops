using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Oops.DataModels;
using Oops.ViewModels;
using Oops.Components;
using static System.Net.Mime.MediaTypeNames;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;

namespace Oops.Daos
{

    public class LogDao : DaoBase
    {
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

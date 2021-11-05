using Dapper;
using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Oops.DataModels;

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
                Console.WriteLine($"get logs use {elapsedSecs} secs");
                return response;
            }
        }

        public void GetOptions(out List<string> services, out List<string> loggers, out List<string> dates)
        {
            DateTime now = DateTime.Now;
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {                
                services = conn.Query<string>("select distinct srv from log order by srv").ToList();
                loggers = conn.Query<string>("select distinct logger from log order by logger").ToList();
                dates = conn.Query<string>("select distinct date from log order by date").ToList();
            }
            var elapsedSecs = (DateTime.Now - now).TotalSeconds.ToString("0.00");
            Console.WriteLine($"get options: {elapsedSecs}");
        }
    }
}

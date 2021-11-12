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
using oops.DataModels;

namespace Oops.Daos
{

    public class ErrorDao : DaoBase
    {
        public List<Error> GetErrorLogs()
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();

                var output = conn.Query<Error>("select * from error_log");
                return output.ToList();
            }
        }
        
        public bool InsertErrorLog(Error errorLog)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                var count = conn.Insert(errorLog);

                return count > 0;
            }
        }

        public Task<int> InsertErrorLogAsync(Error errorLog)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                return conn.InsertAsync(errorLog);
            }
        }

        public async Task<ErrorsResponse> GetLogs(string application, int page, int pageSize)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlCountHeader = "select count(*) from error_log";
                string sqlQueryHeader = "select * from error_log";

                string sqlWhere = string.Empty;
                var parameters = new DynamicParameters();
                if (string.IsNullOrEmpty(application) == false)
                {
                    sqlWhere = " where application=@application";                    
                    parameters.Add("@application", application);
                }

                string sqlCount = string.Format("{0} {1}",
                    sqlCountHeader, sqlWhere);
                ErrorsResponse response = new ErrorsResponse();
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
                    sqlQueryHeader, sqlWhere, startIndex, pageSize);

                List<Error> errors = conn.Query<Error>(sqlQuery, parameters).ToList();
                errors.ForEach(t => t.PrepareData());
                response.Errors = errors;
                return response;
            }
        }

        public async Task<ErrorsResponse> GetLogsBySql(string rawSql)
        {
            ErrorsResponse response = new ErrorsResponse();
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlQuery;
                if (rawSql.Contains("limit"))
                {
                    sqlQuery = rawSql;                    
                } 
                else
                {
                    sqlQuery = rawSql + " limit 0, 100";
                }

                var errors = (await conn.QueryAsync<Error>(rawSql)).ToList();
                errors.ForEach(t => t.PrepareData());
                response.Errors = errors;
                response.CurrentPage = 1;
                response.TotalPage = 1;
                response.TotalRows = errors.Count;
                return response;
            }
        }

        public Error GetLog(int id)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                string sqlQuery = "select * from error_log where id = @id";
                                
                var parameters = new DynamicParameters();
                parameters.Add("@id", id);

                var result = conn.Query<Error>(sqlQuery, parameters).FirstOrDefault();                
                if (result != null)
                {
                    result.PrepareData();
                }
                return result;
            }
        }

        public List<string> GetApplications()
        {            
            string sql = "select distinct application from error_log";

            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                List<string> applications = conn.Query<string>(sql).ToList();
                return applications;
            }            
        }

    }

    public class ErrorsResponse
    {
        public List<Error> Errors { get; set; }
        
        public int CurrentPage { get; set; }
        public int TotalPage { get; set; }
        public int TotalRows { get; set; }
    }

    public class LogsResponse
    {
        public List<OopsLog> Logs { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPage { get; set; }
        public int TotalRows { get; set; }
    }
    public class APIsResponse
    {
        public List<OopsApi> Apis { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPage { get; set; }
        public int TotalRows { get; set; }
    }
}

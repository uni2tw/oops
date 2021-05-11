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

namespace Oops.Daos
{

    public class ErrorDao
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

        public string LoadConnectString()
        {
            string dbDir = Helper.MapPath("db");
            if (System.IO.Directory.Exists(dbDir) == false) {
                System.IO.Directory.CreateDirectory(dbDir);
            }            
            string dbFilePath = System.IO.Path.Combine(dbDir, "my.db");
            return string.Format(@"data source={0};version=3;", dbFilePath);
        }

        public List<Error> GetLogs(string application, int page, int pageSize, 
            out int currentPage, out int totalPage, out int totalRows)
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

                totalRows = conn.ExecuteScalar<int>(sqlCount, parameters);
                totalPage = (totalRows / pageSize) + (totalRows % pageSize == 0 ? 0 : 1);
                if (page > totalPage)
                {
                    currentPage = 1;
                } 
                else
                {
                    currentPage = page;
                }

                int startIndex = (page - 1) * pageSize;
                string sqlQuery = string.Format("{0} {1} order by id desc limit {2},{3}",
                    sqlQueryHeader, sqlWhere, startIndex, pageSize);

                var result = conn.Query<Error>(sqlQuery, parameters).ToList();
                result.ForEach(t => t.PrepareData());
                return result;
            }
        }

        public List<Error> GetLogsBySql(string rawSql)
        {
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

                var result = conn.Query<Error>(rawSql).ToList();
                result.ForEach(t => t.PrepareData());
                return result;
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


}

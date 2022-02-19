using Oops.Components;
using System;
using System.Text.RegularExpressions;

namespace Oops.Daos
{
    public class ConnectionStringBuilder
    {
        Regex regDataSource = new Regex(@"(data source=)([A-Z:\\a-z0-9-{}.\/]*)(;[\w\W]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private string dataSourceHeader { get; set; }
        private string dataSourceDefault { get; set; }
        private string connectionStringWithoutDataSource { get; set; }
        private string lastDataSource;
        public void SetConnectionString(string connectionString)
        {
            var match = regDataSource.Match(connectionString);
            if (match.Success == false)
            {
                throw new Exception("ConnectionString format is illegal.");
            }
            dataSourceHeader = match.Groups[1].Value;
            dataSourceDefault = match.Groups[2].Value;
            connectionStringWithoutDataSource = match.Groups[3].Value;
        }
        public string GetConnectionString(out bool changed)
        {
            changed = false;
            DateTime now = DateTime.Now;
            string dataSource = dataSourceDefault
                .Replace("{yyyy}", now.ToString("yyyy"))
                .Replace("{MM}", now.ToString("MM"))
                .Replace("{dd}", now.ToString("dd"));
             if (lastDataSource != dataSource)
            {
                lastDataSource = dataSource;
                changed = true;
            }
            return $"{dataSourceHeader}{dataSource}{connectionStringWithoutDataSource}";
        }


    }
    public class DaoBase
    {
        public string LastDataSource = "";
        public string lastConnectString ;
        public string dbPath;
        static ConnectionStringBuilder builder;

        public virtual string LoadConnectString()
        {            
            var conn = IoC.Get<IDBConnectionConfig>();

            if (builder == null)
            {
                builder = new ConnectionStringBuilder();
                builder.SetConnectionString(conn.ConnectionString);
            }            
            bool changed;
            var result  = builder.GetConnectionString(out changed);
            if (changed)
            {
                DbUtil.EnsureDBFile(result);
                DbUtil.EnsureTables();
            }
            return result;

        }
    }

    public interface IDBConnectionConfig
    {
        string ConnectionString { get; set; }
    }

    public class DBConnectionConfig : IDBConnectionConfig
    {
        public string ConnectionString { get; set; }
    }

    public interface IDBConnManager
    {
        IDBConnectionConfig GetConnection(string platformId);
    }

}

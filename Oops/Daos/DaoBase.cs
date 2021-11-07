using Oops.Components;

namespace Oops.Daos
{
    public class DaoBase 
    {
        public virtual string LoadConnectString()
        {
            var conn = IoC.Get<IDBConnectionConfig>();
            if (string.IsNullOrEmpty(conn.ConnectionString) == false)
            {
                return conn.ConnectionString;
            }
            string dbDir = Helper.MapPath("db");
            if (System.IO.Directory.Exists(dbDir) == false) {
                System.IO.Directory.CreateDirectory(dbDir);
            }            
            string dbFilePath = System.IO.Path.Combine(dbDir, "my.db");
            return string.Format(@"data source={0};version=3;PRAGMA journal_mode=WAL;", dbFilePath);
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
}

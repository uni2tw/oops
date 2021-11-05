using Oops.Components;

namespace Oops.Daos
{
    public interface IDBConnectionConfig
    {
        string ConnectionString { get; set; }
    }

    public class DBConnectionConfig : IDBConnectionConfig
    {
        public string ConnectionString { get; set; }
    }
    public class DaoBase 
    {
        public virtual string LoadConnectString()
        {
            string connString = IoC.Get<IDBConnectionConfig>().ConnectionString;
            
            if (string.IsNullOrEmpty(connString))
            {                
                string dbDir = Helper.MapPath("db");
                if (System.IO.Directory.Exists(dbDir) == false)
                {
                    System.IO.Directory.CreateDirectory(dbDir);
                }
                string dbFilePath = System.IO.Path.Combine(dbDir, "my.db");
                connString = string.Format(@"data source={0};version=3;PRAGMA journal_mode=WAL;", dbFilePath);
            }            
            return connString;
        }
    }


}

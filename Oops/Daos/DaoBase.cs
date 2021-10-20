using Oops.Components;

namespace Oops.Daos
{
    public class DaoBase 
    {
        public virtual string LoadConnectString()
        {
            string dbDir = Helper.MapPath("db");
            if (System.IO.Directory.Exists(dbDir) == false) {
                System.IO.Directory.CreateDirectory(dbDir);
            }            
            string dbFilePath = System.IO.Path.Combine(dbDir, "my.db");
            return string.Format(@"data source={0};version=3;", dbFilePath);
        }
    }


}

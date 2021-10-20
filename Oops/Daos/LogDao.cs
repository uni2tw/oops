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
    public class LogDao : DaoBase
    {
        public bool InsertLog(Log log)
        {
            using (IDbConnection conn = new SQLiteConnection(LoadConnectString()))
            {
                conn.Open();
                var count = conn.Insert(log);

                return count > 0;
            }
        }
    }
}

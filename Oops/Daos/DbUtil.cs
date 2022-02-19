using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Dapper.Contrib.Extensions;
using oops.DataModels;
using Oops.Components;
using Oops.DataModels;

namespace Oops.Daos
{
    public class DbUtil
    {
        public static bool DeleteTable<T>(string connStr)
        {
            Type type = typeof(T);            
            string tableName = type.GetCustomAttribute<TableAttribute>().Name;

            string sql = $"DROP TABLE IF EXISTS {tableName}";
          
            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                var count = conn.Execute(sql);
                return count >= 0;
            }
        }

        private static bool EnsureTable<T>(string connStr)
        {
            Type type = typeof(T);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            string tableName = type.GetCustomAttribute<TableAttribute>().Name;
            string keyName = string.Empty;
            List<Tuple<string, string, bool>> columns = new List<Tuple<string, string, bool>>();
            foreach (var prop in props)
            {
                if (prop.GetCustomAttribute<ComputedAttribute>() != null)
                {
                    continue;
                }

                var p = prop;
                bool isnull = false;
                string propType = null;

                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propType = prop.PropertyType.GetGenericArguments()[0].Name;
                    isnull = true;
                }
                else
                {
                    propType = prop.Name;
                    isnull = false;
                }

                if (prop.GetCustomAttribute<KeyAttribute>() != null)
                {
                    keyName = prop.Name;
                }
                else
                {
                    if (prop.PropertyType.Name == "String")
                    {
                        propType = "TEXT";
                        isnull = true;
                    }
                    else if (prop.PropertyType.Name == "Int32")
                    {
                        propType = "INTEGER";

                    }
                    else if (prop.PropertyType.Name == "Boolean")
                    {
                        propType = "NUMERIC";
                    }
                    else if (prop.PropertyType.Name == "DateTime")
                    {
                        propType = "DATETIME";
                    }
                    if (propType != null)
                    {
                        columns.Add(new Tuple<string, string, bool>(prop.Name, propType, isnull));
                    }
                }
            }

            StringBuilder sbLog = new StringBuilder();
            sbLog.AppendLine(string.Format(@"
                                CREATE TABLE IF NOT EXISTS {0} (
                                    {1} INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,", tableName, keyName));
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                bool hasComma = (i != columns.Count - 1);
                sbLog.AppendLine(string.Format(@"{0} {1} {2}{3}",
                    column.Item1,
                    column.Item2,
                    column.Item3 ? " NULL" : " NOT NULL",
                    hasComma ? "," : string.Empty));

            }
            sbLog.AppendLine(@");");


            using (IDbConnection conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                var count = conn.Execute(sbLog.ToString());
                return count >= 0;
            }
        }

        public static void EnsureDBFile(string connectionString)
        {
            string dbfile;
            try
            {
                dbfile = Regex.Match(connectionString, @"data source=([A-Z:\\a-z0-9-{}.\/]*)(;[\w\W]*);", RegexOptions.IgnoreCase)
                    .Groups[1].Value;
            }
            catch
            {
                Console.WriteLine("無法解析 connectionString");
                throw;
            }
            if (File.Exists(dbfile) == false)
            {
                try
                {
                    DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(dbfile));
                    if (di.Exists == false)
                    {
                        di.Create();
                    }                    
                    SQLiteConnection.CreateFile(dbfile);
                } 
                catch (Exception)
                {
                    Console.WriteLine("無法建立DB檔案" + dbfile);
                    throw;
                }
            }
        }

        /// <summary>
        /// CREATE UNIQUE INDEX IF NOT EXISTS Error ON IX_Error_Application_Time (com_id,com_name);
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connStr"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        private static bool EnsureIndex<T>(string connStr, params string[] columns)
        {
            if (columns == null || columns.Length == 0)
            {
                return false;
            }
            string tableName = typeof(T).GetCustomAttribute<TableAttribute>().Name;
            string indexName = string.Format("IX_{0}_{1}", tableName, string.Join("_", columns));
            string columnPart = string.Join(",", columns);            
            string rawSql = string.Format("CREATE INDEX IF NOT EXISTS {0} ON {1} ({2});",
                indexName, tableName, columnPart);

            try
            {
                using (IDbConnection conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    conn.Execute(rawSql);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("執行sql失敗:" + rawSql + ", ex=" + ex.ToString());
                return false;
            }
        }

        public static void EnsureTables()
        {
            ErrorDao dao = IoC.Get<ErrorDao>();            

            DbUtil.EnsureTable<Error>(dao.LoadConnectString());
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Time");
            DbUtil.EnsureIndex<Error>(dao.LoadConnectString(), "Application", "Time");

            DbUtil.EnsureTable<OopsLog>(dao.LoadConnectString());
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Date), nameof(OopsLog.Srv), nameof(OopsLog.Logger));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Date), nameof(OopsLog.Logger), nameof(OopsLog.Srv));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Logger), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Srv), nameof(OopsLog.Logger), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Level), nameof(OopsLog.Date));
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.Logger), nameof(OopsLog.Srv), nameof(OopsLog.Date));

            DbUtil.AddTableColumn<OopsLog>(dao.LoadConnectString(), "TraceId", "Text");
            DbUtil.EnsureIndex<OopsLog>(dao.LoadConnectString(), nameof(OopsLog.TraceId));

            DbUtil.EnsureTable<OopsApi>(dao.LoadConnectString());
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Date), nameof(OopsApi.Url));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Date), nameof(OopsApi.Srv));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Srv), nameof(OopsApi.Url));
            DbUtil.EnsureIndex<OopsApi>(dao.LoadConnectString(), nameof(OopsApi.Url));

            DbUtil.EnsureTable<OopsLogOption>(dao.LoadConnectString());
            IoC.Get<LogDao>().UpgradeForFastOption().Wait();
        }

        public static void AddTableColumn<T>(string connStr, string fieldName, string fieldType)
        {
            bool isExist = false;


            string tableName = typeof(T).GetCustomAttribute<TableAttribute>().Name;

            string sqlRaw = string.Empty;
            try
            {
                using (SQLiteConnection conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    sqlRaw = string.Format("Select * from {0} limit 1", tableName);

                    var tableRow = conn.ExecuteReader(sqlRaw, null);

                    for (int i = 0; i < tableRow.FieldCount; i++)
                    {
                        if (isExist)
                            break;

                        if (fieldName == tableRow.GetName(i))
                        {
                            isExist = true;
                        }
                    }

                    if (!isExist)
                    {
                        sqlRaw = string.Format("ALTER TABLE {0} ADD COLUMN {1} {2} NULL", tableName, fieldName, fieldType);

                        using (SQLiteCommand cmd = new SQLiteCommand(sqlRaw, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        isExist = true;
                    }

                    tableRow.Close();
                    tableRow.Dispose();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("執行sql失敗:" + sqlRaw + ", ex=" + ex.ToString());
            }
        }
    }
}


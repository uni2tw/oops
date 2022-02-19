using System.Collections.Generic;
using System.Data.SQLite;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text;
using System;
using System.Linq;

namespace oops.Daos
{
    public static class SqliteExtensnios
    {
        public static void BulkInsert<T>(this SQLiteConnection conn, IList<T> items, string tableName = null) where T : new()
        {
            int sqliteLimitInsertMultipleRows = 500;
            for (int i = 0; i < items.Count; i = i + sqliteLimitInsertMultipleRows)
            {
                DoBulkInsert(conn, items.Skip(i).Take(sqliteLimitInsertMultipleRows).ToList());
            }
        }
        private static void DoBulkInsert<T>(this SQLiteConnection conn, IList<T> items, string tableName = null) where T : new()
        {
            if (tableName == null)
            {
                var tableAttr = typeof(T).GetCustomAttribute<Dapper.Contrib.Extensions.TableAttribute>();
                if (tableAttr != null)
                {
                    tableName = tableAttr.Name;
                }
                else
                {
                    tableName = nameof(T);
                }
            }

            SQLiteCommand cmd = conn.CreateCommand();
            List<string> args = new List<string>();
            ParseInseretSql<T>(tableName, args, out string sqlHead);
            cmd.CommandText += sqlHead;

            StringBuilder sqlBody = new StringBuilder();

            int i = 0;
            Dictionary<string, PropertyInfo> propMaps = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var propInfo in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                propMaps.Add(propInfo.Name, propInfo);
            }
            foreach (var item in items)
            {
                if (i > 0)
                {
                    sqlBody.Append(",");
                }
                sqlBody.Append("(");

                for (int j = 0; j < args.Count; j++)
                {
                    string arg = args[j];
                    if (j > 0)
                    {
                        sqlBody.Append(',');
                    }
                    string argName = $"@{arg}_{i}";
                    var prop = propMaps[arg];
                    object val = prop.GetValue(item);
                    cmd.Parameters.AddWithValue(argName, val);
                    sqlBody.Append(argName);
                }
                sqlBody.Append(")");
                i++;
            }
            cmd.CommandText += " VALUES " + sqlBody.ToString() + ";";
            var result = cmd.ExecuteScalar();
        }
        private static bool ParseInseretSql<T>(string tableName, List<string> args, out string insertSqlHead)
        {
            Dictionary<string, PropertyInfo> propMaps = new Dictionary<string, PropertyInfo>();
            List<Tuple<string, string>> propColumns = new List<Tuple<string, string>>();
            foreach (var propInfo in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var keyAttr = propInfo.GetCustomAttribute<Dapper.Contrib.Extensions.KeyAttribute>();
                if (keyAttr != null)
                {
                    continue;
                }
                propMaps.Add(propInfo.Name, propInfo);

                var aliasAttr = propInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (aliasAttr == null)
                {
                    propColumns.Add(new Tuple<string, string>(propInfo.Name, propInfo.Name));
                }
                else
                {
                    propColumns.Add(new Tuple<string, string>(propInfo.Name, aliasAttr.Name));
                }
            }

            StringBuilder sbSql = new StringBuilder();
            sbSql.Append("INSERT INTO ");
            sbSql.Append(tableName);
            sbSql.AppendLine("(");
            for (int i = 0; i < propColumns.Count; i++)
            {
                var pair = propColumns[i];
                if (i > 0)
                {
                    sbSql.AppendLine(",");
                }
                sbSql.Append("`");
                sbSql.Append(pair.Item2);
                sbSql.Append("`");

                args.Add(pair.Item1);
            }
            sbSql.AppendLine(")");

            insertSqlHead = sbSql.ToString();

            return true;
        }

    }

}

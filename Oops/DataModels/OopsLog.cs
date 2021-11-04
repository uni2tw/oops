using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using Dapper.Contrib.Extensions;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace Oops.DataModels
{
    [Table("log")]
    public class OopsLog
    {
        public const string _TOPIC = "log";
        public OopsLog()
        {

        }
        [Key]
        public int Id { get; set; }
        public string Env { get; set; }
        public string Srv { get; set; }
        public string Host { get; set; }
        public string Logger { get; set; }
        public int Level { get; set; }
        public string Message { get; set; }
        //20211201 查詢用
        public string Date { get; set; }
        //2021121006 查詢用
        public string DateHour { get; set; }
        public DateTime Time { get; set; }
    }

    [Table("env")]
    public class OopsEnv
    {
        public string Name { get; set; }
    }

    [Table("service")]
    public class OopsService
    {
        public string Name { get; set; }
    }

    [Table("logger")]
    public class OopsLogger
    {
        public string Name { get; set; }
    }

    public enum OopsLogLevel
    {
        Trace = 0, Debug = 1, Info = 2, Warn = 3, Error = 4, Fatal = 5, Off = 6
    }
}

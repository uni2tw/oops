using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using Dapper.Contrib.Extensions;

namespace Oops.DataModels
{
    [Table("log")]
    public class Log
    {        
        [Key]
        public int Id { get; set; }
        public string SrvCode { get; set; }
        public string HostName { get; set; }
        public int Level { get; set; }
        public string LoggerName { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
    }
}

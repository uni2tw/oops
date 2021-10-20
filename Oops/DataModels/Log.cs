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

        public Log()
        {           
        }

        [Key]
        public int Id { get; set; }        
        public string HostName { get; set; }        
        public string FullType { get; set; }
        public string Type1 { get; set; }
        public string Type2 { get; set; }
        public string Type3 { get; set; }
        public string Type4 { get; set; }
        public string Type5 { get; set; }
        public string Message { get; set; }
        public decimal? NumberOpt1 { get; set; }
        public decimal? NumberOpt2 { get; set; }
        public decimal? NumberOpt3 { get; set; }
        public string StringOpt1 { get; set; }
        public DateTime Time { get; set; }

    }
}

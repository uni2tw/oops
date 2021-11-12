using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.Builder;
using System;

namespace oops.DataModels
{   
    [Table("api")]
    public class OopsApi
    {
        public const string _TOPIC = "api";
        public OopsApi()
        {

        }
        [Key]
        public int Id { get; set; }
        public string Srv { get; set; }
        public string Host { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public string Error { get; set; }
        public string Date { get; set; }
        public DateTime Time { get; set; }
    }
}
